const document = globalThis.document;
if (typeof SharedArrayBuffer !== "function") {
    const errorString = "This page requires a browser that supports SharedArrayBuffer. We recommend using the latest version of Chrome.\n该网页需要运行在支持 SharedArrayBuffer 的浏览器上，推荐使用最新版的 Chrome";
    document.body.innerText = errorString;
    throw new Error(errorString);
}

if ("serviceWorker" in globalThis.navigator) {
    try {
        await globalThis.navigator.serviceWorker.register("./service-worker.js", { scope: "./" });
        await globalThis.navigator.serviceWorker.ready;
    } catch (error) {
        console.error(`Register service worker failed: ${error}`);
    }
}

const busyBar = document.getElementById("splashBusyBar");
let litIndex = 0;
const busyBarInterval = globalThis.setInterval(() => {
    busyBar.children[litIndex].classList.remove("lit");
    litIndex = (litIndex + 1) % 5;
    busyBar.children[litIndex].classList.add("lit");
}, 250);

const { dotnet } = await import('./_framework/dotnet.js');

const canvas = document.getElementById("canvas");
let sharedInputMemoryPtr = 0;
let sharedContentMemoryPtr = 0;
const contentPendingChunks = [];
let contentWrittenOffset = 8;
let contentDownloaded = false;

async function downloadContent() {
    const response = await globalThis.fetch("./assets/Content.zip");
    const reader = response.body.getReader();
    while (true) {
        const { done, value } = await reader.read();
        if (done) {
            contentDownloaded = true;
            if (sharedContentMemoryPtr !== 0) {
                const view = new Uint8Array(runtime.Module.wasmMemory.buffer);
                Atomics.store(view, sharedContentMemoryPtr, 1);
            }
            break;
        }
        if (sharedContentMemoryPtr === 0){
            contentPendingChunks.push(value);
        } else {
            const view = new Uint8Array(runtime.Module.wasmMemory.buffer);
            view.set(value, sharedContentMemoryPtr + contentWrittenOffset);
            contentWrittenOffset += value.length;
        }
    }
}
downloadContent().then();

// TODO: 加上 withResourceLoader
const runtime = await dotnet.withModuleConfig({
    //设置 C# worker 中的 Module.canvas，它会自己将其转换为 OffscreenCanvas
    canvas: canvas,
    INITIAL_MEMORY: 386662400
}).create();
globalThis.dotnetRuntime = runtime;
const engineExports = await runtime.getAssemblyExports("Engine.dll");
const interop = engineExports.Engine.Browser.BrowserInterop; // 调用这个特别慢，屏幕刷新率过高甚至会因此导致卡顿，能少用就少用

let needPointerLock = false;
function checkAndRequestPointerLock(){
    if (needPointerLock) {
        if (document.pointerLockElement !== canvas && canvas.requestPointerLock) {
            return canvas.requestPointerLock({ unadjustedMovement: true }).catch(error => {
                if (error?.name === "NotSupportedError") {
                    // 有些平台可能不支持未调整的移动，尝试重新请求常规指针锁定。
                    return canvas.requestPointerLock();
                }
            });
        }
    }
    else if (document.pointerLockElement === canvas && document.exitPointerLock) {
        document.exitPointerLock();
    }
}

// ---------------------------------------------------------------
// Input System Setup
// ---------------------------------------------------------------

// 布局常量 (需与 C# 严格一致)
const STRUCT = {
    HEADER_SIZE: 8, // ActiveIndex(4) + Padding(4)
    // InputBuffer 偏移量:
    OFF_CANVAS_WIDTH: 0,
    OFF_CANVAS_HEIGHT: 4,
    OFF_DEVICE_PIXEL_RATIO: 8,
    OFF_MOUSE_POSITION_X: 16,
    OFF_MOUSE_POSITION_Y: 20,
    OFF_GP_AXES: 24,    // 16 floats
    OFF_GP_TRIGGERS: 88, // 8 floats
    OFF_USED_BYTES: 120,
    OFF_EVENT_DATA: 124,
    BUFFER_SIZE: 4220 // 一个 Buffer 的总大小，124 + 4096
};

let canvasWidth = 1;
let canvasHeight = 1;
let mousePositionX = 0;
let mousePositionY = 0;

// 手柄上一帧的按键状态掩码
const lastGamepadBtnMasks = [0, 0, 0, 0];

function getWritePtr(sharedInputView) {
    const activeIndex = sharedInputView.getInt32(sharedInputMemoryPtr, true);
    return sharedInputMemoryPtr + STRUCT.HEADER_SIZE + (activeIndex * STRUCT.BUFFER_SIZE);
}

// --- 写入 4字节事件 ---
function writeSmallEvent(type, param = 0, payload = 0, dataView = null) {
    // 这个 buffer 是一个 SharedArrayBuffer，因为指针可能会变，所以每次都要重新获取；另外，sharedInputMemoryPtr 不会变
    dataView ??= new DataView(runtime.Module.wasmMemory.buffer);
    const base = getWritePtr(dataView);
    const usedBytesOffset = base + STRUCT.OFF_USED_BYTES;
    const used = dataView.getInt32(usedBytesOffset, true);
    const newUsed = used + 4;
    if (newUsed > 4096) {
        return;
    }
    const ptr = base + STRUCT.OFF_EVENT_DATA + used;
    // 写入: Type(1) + Param(1) + Payload(2)
    // DataView 没有 setInt8/16 这种混合写，直接写一个 Int32 最快
    // 内存结构: [Type | Param | PayloadL | PayloadH] (Little Endian)
    // Packed: Type | (Param << 8) | (Payload << 16)
    const packed = (type & 0xFF) | ((param & 0xFF) << 8) | ((payload & 0xFFFF) << 16);
    dataView.setInt32(ptr, packed, true);
    dataView.setInt32(usedBytesOffset, newUsed, true);
}

// --- 写入 12字节事件 ---
function writeLargeEvent(type, param = 0, payload = 0, x = 0, y = 0, dataView = null) {
    dataView ??= new DataView(runtime.Module.wasmMemory.buffer);
    const base = getWritePtr(dataView);
    const usedBytesOffset = base + STRUCT.OFF_USED_BYTES;
    const used = dataView.getInt32(usedBytesOffset, true);
    const newUsed = used + 12;
    if (newUsed > 4096) {
        return;
    }
    const ptr = base + STRUCT.OFF_EVENT_DATA + used;
    // 1. Header (4 bytes)
    const packed = (type & 0xFF) | ((param & 0xFF) << 8) | ((payload & 0xFFFF) << 16);
    dataView.setInt32(ptr, packed, true);
    // 2. Floats (8 bytes)
    dataView.setFloat32(ptr + 4, x, true);
    dataView.setFloat32(ptr + 8, y, true);
    dataView.setInt32(usedBytesOffset, newUsed, true);
}

function pollInputLoop() {
    const dataView = new DataView(runtime.Module.wasmMemory.buffer);
    const base = getWritePtr(dataView);
    // 1. 写入公共状态
    dataView.setFloat32(base + STRUCT.OFF_CANVAS_WIDTH, canvasWidth, true);
    dataView.setFloat32(base + STRUCT.OFF_CANVAS_HEIGHT, canvasHeight, true);
    dataView.setFloat32(base + STRUCT.OFF_DEVICE_PIXEL_RATIO, globalThis.devicePixelRatio, true);
    dataView.setFloat32(base + STRUCT.OFF_MOUSE_POSITION_X, mousePositionX, true);
    dataView.setFloat32(base + STRUCT.OFF_MOUSE_POSITION_Y, mousePositionY, true);
    // 2. 手柄
    const gamepads = globalThis.navigator.getGamepads();
    for (let i = 0; i < gamepads.length; i++) {
        const gamepad = gamepads[i];
        if (!gamepad || !gamepad.connected || gamepad.mapping !== "standard") {
            continue;
        }
        const gamepadIndex = gamepad.index;
        if (gamepadIndex < 0 || gamepadIndex >= 4) {
            continue;
        }
        // --- A. 写入摇杆和扳机状态 (Snapshot) ---
        const axes = gamepad.axes;
        for (let a = 0; a < 4; a++) {
            dataView.setFloat32(base + STRUCT.OFF_GP_AXES + (gamepadIndex * 16) + (a * 4), axes[a], true);
        }
        const buttons = gamepad.buttons;
        dataView.setFloat32(base + STRUCT.OFF_GP_TRIGGERS + (gamepadIndex * 8), buttons[6].value, true);
        dataView.setFloat32(base + STRUCT.OFF_GP_TRIGGERS + (gamepadIndex * 8) + 4, buttons[7].value, true);
        // --- B. 按键事件检测 ---
        let currMask = 0;
        // 跳过 6、7、16
        for (let b = 0; b < 6; b++) {
            if (buttons[b].pressed) {
                currMask |= (1 << b);
            }
        }
        for (let b = 8; b < 16; b++) {
            if (buttons[b].pressed) {
                currMask |= (1 << b);
            }
        }
        const prevMask = lastGamepadBtnMasks[i];
        const changes = currMask ^ prevMask; // 异或找出变化的位
        if (changes !== 0) {
            for (let b = 0; b < 6; b++) {
                if ((changes & (1 << b)) !== 0) {
                    const isDown = (currMask & (1 << b)) !== 0;
                    writeSmallEvent(isDown ? 3 : 4, translateGamepadButtons(b), gamepadIndex, dataView);
                }
            }
            for (let b = 8; b < 16; b++) {
                if ((changes & (1 << b)) !== 0) {
                    const isDown = (currMask & (1 << b)) !== 0;
                    writeSmallEvent(isDown ? 3 : 4, translateGamepadButtons(b), gamepadIndex, dataView);
                }
            }
            lastGamepadBtnMasks[i] = currMask;
        }
    }
    globalThis.requestAnimationFrame(pollInputLoop);
}

function translateKeyCode(code) {
    switch (code) {
        case "ShiftLeft":
        case "ShiftRight":
            return 1;
        case "ControlLeft":
        case "ControlRight":
            return 2;
        case "F1":
            return 3;
        case "F2":
            return 4;
        case "F3":
            return 5;
        case "F4":
            return 6;
        case "F5":
            return 7;
        case "F6":
            return 8;
        case "F7":
            return 9;
        case "F8":
            return 10;
        case "F9":
            return 11;
        case "F10":
            return 12;
        case "F11":
            return 13;
        case "F12":
            return 14;
        case "ArrowLeft":
            return 15;
        case "ArrowRight":
            return 16;
        case "ArrowUp":
            return 17;
        case "ArrowDown":
            return 18;
        case "Enter":
        case "NumpadEnter":
            return 19;
        case "Escape":
            return 20;
        case "Space":
            return 21;
        case "Tab":
            return 22;
        case "Backspace":
            return 23;
        case "Insert":
            return 24;
        case "Delete":
            return 25;
        case "PageUp":
            return 26;
        case "PageDown":
            return 27;
        case "Home":
            return 28;
        case "End":
            return 29;
        case "CapsLock":
            return 30;
        case "KeyA":
            return 31;
        case "KeyB":
            return 32;
        case "KeyC":
            return 33;
        case "KeyD":
            return 34;
        case "KeyE":
            return 35;
        case "KeyF":
            return 36;
        case "KeyG":
            return 37;
        case "KeyH":
            return 38;
        case "KeyI":
            return 39;
        case "KeyJ":
            return 40;
        case "KeyK":
            return 41;
        case "KeyL":
            return 42;
        case "KeyM":
            return 43;
        case "KeyN":
            return 44;
        case "KeyO":
            return 45;
        case "KeyP":
            return 46;
        case "KeyQ":
            return 47;
        case "KeyR":
            return 48;
        case "KeyS":
            return 49;
        case "KeyT":
            return 50;
        case "KeyU":
            return 51;
        case "KeyV":
            return 52;
        case "KeyW":
            return 53;
        case "KeyX":
            return 54;
        case "KeyY":
            return 55;
        case "KeyZ":
            return 56;
        case "Numpad0":
        case "Digit0":
            return 57;
        case "Numpad1":
        case "Digit1":
            return 58;
        case "Numpad2":
        case "Digit2":
            return 59;
        case "Numpad3":
        case "Digit3":
            return 60;
        case "Numpad4":
        case "Digit4":
            return 61;
        case "Numpad5":
        case "Digit5":
            return 62;
        case "Numpad6":
        case "Digit6":
            return 63;
        case "Numpad7":
        case "Digit7":
            return 64;
        case "Numpad8":
        case "Digit8":
            return 65;
        case "Numpad9":
        case "Digit9":
            return 66;
        case "Backquote":
            return 67;
        case "Minus":
        case "NumpadSubtract":
            return 68;
        case "Equal":
        case "NumpadAdd":
            return 69;
        case "BracketLeft":
            return 70;
        case "BracketRight":
            return 71;
        case "Semicolon":
            return 72;
        case "Quote":
            return 73;
        case "Comma":
            return 74;
        case "Period":
        case "NumpadDecimal":
            return 75;
        case "Slash":
        case "NumpadDivide":
            return 76;
        case "AltLeft":
        case "AltRight":
            return 77;
        case "Backslash":
            return 78;
        default:
            return -1;
    }
}

function translateMouseButton(button) {
    switch (button) {
        case 1:
            return 2;
        case 2:
            return 1;
        default:
            return button;
    }
}

function translateGamepadButtons(button) {
    switch (button) {
        case 0:
            return 0;
        case 1:
            return 1;
        case 2:
            return 2;
        case 3:
            return 3;
        case 4:
            return 8;
        case 5:
            return 9;
        case 8:
            return 4;
        case 9:
            return 5;
        case 10:
            return 6;
        case 11:
            return 7;
        case 12:
            return 11;
        case 13:
            return 13;
        case 14:
            return 10;
        case 15:
            return 12;
        default:
            return -1;
    }
}

// ---------------------------------------------------------------
// Set Module Imports
// ---------------------------------------------------------------

runtime.setModuleImports("main.js", {
    initialize: inputPtr => {
        sharedInputMemoryPtr = inputPtr;

        const observer = new ResizeObserver(entries => {
            const entry = entries[0];
            const devicePixelRatio = globalThis.devicePixelRatio || 1.0;
            // 注意：这里我们测量的是 DOM 元素的显示尺寸
            if (entry.devicePixelContentBoxSize) {
                // 如果浏览器支持直接获取物理像素尺寸
                canvasWidth = entry.devicePixelContentBoxSize[0].inlineSize;
                canvasHeight = entry.devicePixelContentBoxSize[0].blockSize;
            } else {
                // 降级方案
                const rect = canvas.getBoundingClientRect();
                canvasWidth = Math.round(rect.width * devicePixelRatio);
                canvasHeight = Math.round(rect.height * devicePixelRatio);
            }
            const runningWorkers = runtime.Module.PThread?.runningWorkers;
            if (runningWorkers && runningWorkers.length > 0) {
                runningWorkers[0].postMessage({
                    cmd: 'resize_canvas',
                    width: canvasWidth,
                    height: canvasHeight
                });
            }
        });
        observer.observe(canvas);

        const keyDown = e => {
            e.stopPropagation();
            let translatedKeyCode = translateKeyCode(e.code);
            if (translatedKeyCode >= 0) {
                writeSmallEvent(1, translatedKeyCode, e.key.length === 1 ? e.key.charCodeAt(0) : 0);
            }
        }

        const keyUp = e => {
            e.stopPropagation();
            let translatedKeyCode = translateKeyCode(e.code);
            if (translatedKeyCode >= 0) {
                writeSmallEvent(2, translatedKeyCode);
            }
        }

        const pointerDown = e => {
            e.preventDefault();
            e.stopPropagation();
            canvas.focus();
            const devicePixelRatio = globalThis.devicePixelRatio || 1.0;
            switch (e.pointerType) {
                case "mouse":
                case "pen":
                    checkAndRequestPointerLock();
                    writeLargeEvent(128, translateMouseButton(e.button), 0, e.offsetX * devicePixelRatio, e.offsetY * devicePixelRatio);
                    break;
                case "touch":
                    writeLargeEvent(132, e.pointerId % 10, 0, e.offsetX * devicePixelRatio, e.offsetY * devicePixelRatio);
                    break;
            }
        }

        const pointerMove = e => {
            e.preventDefault();
            e.stopPropagation();
            const devicePixelRatio = globalThis.devicePixelRatio || 1.0;
            switch (e.pointerType) {
                case "mouse":
                case "pen":
                    mousePositionX = e.offsetX * devicePixelRatio;
                    mousePositionY = e.offsetY * devicePixelRatio;
                    writeLargeEvent(130, 0, 0, e.movementX, e.movementY);
                    break;
                case "touch":
                    writeLargeEvent(134, e.pointerId % 10, 0, e.offsetX * devicePixelRatio, e.offsetY * devicePixelRatio);
                    break
            }
        }

        const pointerUp = e => {
            e.preventDefault();
            e.stopPropagation();
            switch (e.pointerType) {
                case "mouse":
                case "pen":
                    writeLargeEvent(129, translateMouseButton(e.button), 0, e.offsetX * devicePixelRatio, e.offsetY * devicePixelRatio);
                    break;
                case "touch":
                    writeLargeEvent(133, e.pointerId % 10, 0, e.offsetX * devicePixelRatio, e.offsetY * devicePixelRatio);
                    break;
            }
        }

        const mouseWheel = e => {
            e.preventDefault();
            e.stopPropagation();
            writeLargeEvent(131, 0, 0, e.deltaX / 100, e.deltaY / 100);
        }

        const gamepadConnected = e => {
            let gamepad = e.gamepad;
            if (gamepad !== null && gamepad.index >= 0 && gamepad.index < 4) {
                // id 是字符串，所以还是走 interop
                interop.OnGamepadConnected(gamepad.index, gamepad.id);
            }
        }

        const gamepadDisconnected = e => {
            let gamepad = e.gamepad;
            if (gamepad !== null && gamepad.index >= 0 && gamepad.index < 4) {
                lastGamepadBtnMasks[gamepad.index] = 0;
                writeSmallEvent(6, 0, gamepad.index);
            }
        }

        const pointerLockChange = () => {
            if (needPointerLock && document.pointerLockElement !== canvas) {
                //20：Escape
                writeSmallEvent(1, 20);
                writeSmallEvent(2, 20);
            }
        }

        const drop = async e => {
            e.preventDefault();
            e.stopPropagation();
            if (e.dataTransfer.files.length > 0) {
                const file = e.dataTransfer.files[0];
                const buffer = await file.arrayBuffer();
                interop.OnDrop(new Uint8Array(buffer), file.name);
            }
        }

        const visibilityChange = () => {
            writeSmallEvent(64, document.visibilityState === "visible" ? 1 : 0);
        };

        const focus = () => {
            writeSmallEvent(64, 1);
        };

        const blur = () => {
            writeSmallEvent(64, 0);
        };

        const fullscreenChange = () => {
            writeSmallEvent(65, document.fullscreenElement === canvas ? 1 : 0);
        };

        const popState = e => {
            e.preventDefault();
            //20：Escape
            writeSmallEvent(1, 20);
            writeSmallEvent(2, 20);
            globalThis.history.pushState(null, "", globalThis.location.href);
        }

        canvas.addEventListener("contextmenu", e => e.preventDefault(), false);
        canvas.addEventListener("keydown", keyDown, false);
        canvas.addEventListener("keyup", keyUp, false);
        canvas.addEventListener("pointerdown", pointerDown, false);
        canvas.addEventListener("pointermove", pointerMove, false);
        canvas.addEventListener("pointerup", pointerUp, false);
        canvas.addEventListener("wheel", mouseWheel, false);
        globalThis.addEventListener("gamepadconnected", gamepadConnected, false);
        globalThis.addEventListener("gamepaddisconnected", gamepadDisconnected, false);
        document.addEventListener("pointerlockchange", pointerLockChange, false);
        canvas.addEventListener("drop", drop, false);
        canvas.addEventListener("dragover", e => e.preventDefault(), false);
        document.addEventListener("visibilitychange", visibilityChange, false);
        canvas.addEventListener("focus", focus, false);
        canvas.addEventListener("blur", blur, false);
        document.addEventListener("fullscreenchange", fullscreenChange, false);
        globalThis.addEventListener("popstate", popState, false);

        globalThis.history.pushState(null, "", globalThis.location.href);
        //interop.SetHostedHref(globalThis.location.href);
        canvas.focus();
        if (document.fullscreenElement === canvas) {
            writeSmallEvent(65, 1);
        }
        pollInputLoop();
    },
    getTitle: () => document.title,
    setTitle: title => document.title = title,
    getLanguage: () => globalThis.navigator.language,
    close: () => globalThis.close(),
    reload: () => globalThis.location.reload(),
    setDocumentLang : lang => document.documentElement.lang = lang,
    openUrlInNewTab: url => globalThis.open(url, "_blank"),
    setNeedPointerLock: need => {
        needPointerLock = need;
    },
    showOpenFilePicker: async (descAndExtArray, extCounts, defaultPath) => {
        let types = [];
        let index = 0;
        for (let i = 0; i < extCounts.length; i++) {
            const desc = descAndExtArray[index++];
            const count = extCounts[i];
            let extensions = [];
            for (let j = 0; j < count; j++) {
                extensions.push(descAndExtArray[index++]);
            }
            types.push({
                description: desc,
                accept: {
                    "*/*": extensions
                }
            });
        }
        let pickerOption = { multiple: false };
        if (types.length > 0) {
            pickerOption.types = types;
            pickerOption.excludeAcceptAllOption = true;
        }
        if (defaultPath) {
            pickerOption.startIn = defaultPath;
        }
        let fileHandles = await globalThis.showOpenFilePicker(pickerOption);
        if (fileHandles.length > 0) {
            const fileHandle = fileHandles[0];
            return fileHandle.getFile();
        }
        return null;
    },
    getFileName: file => file?.name ?? "",
    getFileBytes: async file => {
        if (file === null) {
            return [];
        }
        const buffer = await file.arrayBuffer();
        return new Uint8Array(buffer);
    },
    returnSelf: value => value,
    showSaveFilePicker: async (fileName, mimeType) => {
        if (mimeType === null) {
            return globalThis.showSaveFilePicker({
                suggestedName: fileName
            });
        }
        return globalThis.showSaveFilePicker({
            suggestedName: fileName,
            types: [
                {
                    description: "",
                    accept: {
                        [mimeType]: []
                    }
                }
            ]
        });
    },
    saveBytesToFileHandle: async (fileHandle, bytes) => {
        if (fileHandle === null || bytes === null || bytes.length === 0) {
            return;
        }
        const writable = await fileHandle.createWritable();
        await writable.write(bytes);
        await writable.close();
    },
    setFullscreen: async (flag) => {
        if (flag) {
            if (document.fullscreenElement !== canvas) {
                await canvas.requestFullscreen({navigationUI: "hide"});
                await globalThis.screen?.orientation?.lock("landscape"); // 经常无效
            }
        }
        else {
            await document.exitFullscreen();
        }
    },
    showKeyboard: (title, defaultText) => {
        return globalThis.prompt(title, defaultText);
    },
    setContentPtr: ptr => {
        sharedContentMemoryPtr = ptr;
        const view = new Uint8Array(runtime.Module.wasmMemory.buffer);
        for (const chunk of contentPendingChunks) {
            view.set(chunk, sharedContentMemoryPtr + contentWrittenOffset);
            contentWrittenOffset += chunk.length;
        }
        contentPendingChunks.length = 0;
        if (contentDownloaded) {
            Atomics.store(view, sharedContentMemoryPtr, 1);
        }
    },
    firstFramePrepared: () => {
        globalThis.clearInterval(busyBarInterval);
        document.getElementById("splash")?.remove();
    },
});
await runtime.runMain(runtime.getConfig().mainAssemblyName);