import os
import chardet

def convert_to_utf8(file_path):
    """检测并转换文件编码为 UTF-8"""
    # 读取文件的原始二进制数据
    with open(file_path, 'rb') as f:
        raw_data = f.read()
        result = chardet.detect(raw_data)
        encoding = result['encoding']

    # 跳过已经是 UTF-8 的文件
    if encoding and encoding.lower() in ['utf-8', 'ascii']:
        print(f"[✓] Skipped {file_path} (encoding: {encoding})")
        return

    # 其他编码的文件转换为 UTF-8
    try:
        with open(file_path, 'r', encoding=encoding, errors='ignore') as f:
            content = f.read()
        with open(file_path, 'w', encoding='utf-8', newline='\n') as f:
            f.write(content)
        print(f"[🔥] Converted {file_path} from {encoding} to UTF-8")
    except Exception as e:
        print(f"[⚠] Failed to convert {file_path} (encoding: {encoding}) - {e}")

def process_directory(root_dir):
    """遍历项目中的所有 .cs 文件"""
    for root, _, files in os.walk(root_dir):
        for file in files:
            if file.endswith('.cs'):
                convert_to_utf8(os.path.join(root, file))

if __name__ == "__main__":
    project_path = "."  # 当前目录
    process_directory(project_path)
    print("\n✅ 所有非 UTF-8 文件已转换为 UTF-8！")
