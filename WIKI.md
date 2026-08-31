# Phytomana Wiki

*A player-facing guide to the Mana systems added by **Phytomana** for **SurvivalCraft2**.*

---

# Units

Mana is measured in **Mn**. Every block that holds mana has a fixed capacity.

| Block | Capacity |
| --- | --- |
| Sunblaze Flower | 800 Mn |
| Springfroth Flower | 240 Mn |
| Mana Spreader | 1200 Mn |
| Mana Pool | 3800 Mn |

---

# Sunblaze Flower *(日耀花)*

A furnace-like flower that **burns fuel to produce Mana**. Throw any burnable item onto it and it burns for the duration of that fuel.

## Production

While burning, the flower produces:

| Fuel Heat Level | Output |
| --- | --- |
| 1 | ≈ 3.6 Mn/s |
| 2 | ≈ 4.8 Mn/s |
| 3 | ≈ 6.1 Mn/s |
| 4 | ≈ 7.3 Mn/s |

## Example Output

One piece of **Grown Wood** (Heat Level 1, burns 20 s) yields roughly **73 Mn**. Higher-heat fuels (2+ in-game burning materials) yield proportionally more.

## Usage

- Place the flower within one block of a **Mana Spreader** (same height, 3 × 3 area) and it automatically pushes Mana into it.
- If the flower sits alone with no spreader nearby, and nothing burns for a while, it slowly leaks stored Mana.
- Click it with a **Grown Staff** to read its live status.

> **Crafting:** none yet — currently creative/development only.

---

# Springfroth Flower *(水绣球)*

The opposite of the Sunblaze — it turns **water into Mana**.

## Production

It must be within one block (same height, 3 × 3 area) of a still water source (*level 0*). On absorbing a water source it converts it for **3 seconds**, producing **≈ 4.2 Mn/s** while absorbing.

## Example Output

One water source block yields roughly **13 Mn** per absorb cycle. Place a new water source to trigger the next cycle.

## Usage

- Same flow as the Sunblaze Flower: keep a **Mana Spreader** adjacent so the Mana is pushed out automatically (up to **70 Mn/s**).
- Click it with a **Grown Staff** for a live readout.

> **Crafting:** none yet — currently creative/development only.

---

# Mana Spreader *(魔力发射器)*

The **router** of the Mana network. Flowers push Mana into it, and the **Grown Staff** rewires it to send Mana onward.

- Capacity: **1200 Mn**.
- Automatically receives Mana from any producing flower in the adjacent 3 × 3 area on its height.
- Send Mana to another Spreader **or** a Mana Pool with a **Grown Staff** link (see below) at **160 Mn/s per link**.

## Crafting

![recipe shapes below]

| a | b | c |
| --- | --- | --- |
| Grown Wood | Iron Ingot | Sumeru Petal |

```
aaa | aaa | aaa
cb  | cb  | cb
aaa | aaa | aaa
```

> 7 × **Grown Wood**, 1 × **Iron Ingot**, 1 × **Sumeru Petal** → 1 × **Mana Spreader**.

---

# Mana Pool *(魔力池)*

A passive, high-capacity **Mana tank**.

- Capacity: **3800 Mn**.
- Holds Mana but **never discharges on its own** — it only receives what is linked into it.

## Crafting

```
   | a a | aaa
aaa | a a | aaa
```

> 5 × **Grown Stone** → 1 × **Mana Pool**.

## Mana Ingot

Throw an **Iron Ingot** item into a pool that holds at least **300 Mn**:

- The pool consumes **300 Mn**.
- The Iron Ingot is consumed and replaced by a **Mana Ingot** at the same spot.
- Four Tianyi-blue sparks mark the conversion.

> **Crafting:** Mana Ingot itself has no crafting recipe yet.

---

# Grown Staff *(生息法杖)*

The **remote control** for the whole mana network. Sneak + click (hold) to switch between two modes.

## Crafting

```
a b | a b | a b
ba  | ba  | ba
  a |   a |   a
```

> 3 × **Grown Wood**, 2 × **Sumeru Petal** → 1 × **Grown Staff**.

## Work Mode (default)

Right-click any mana block to read its status:

- **Sunblaze / Springfroth Flower** — stored Mana, working/idle state, and net production.
- **Mana Spreader** — stored Mana and current outgoing usage per link.

## Bind Mode

Rewire the network. Click **a Mana Spreader** to start (pink highlight), then click **another Spreader or a Mana Pool** to finish (blue highlight):

- Distance must be **≤ 16 blocks**.
- The source and target must lie on **one straight axis**.
- On success the source Spreader points at its target and a white Mana flow starts — up to **160 Mn/s** per link.

> Clicking the same block twice cancels the selection. Links keep running as long as both ends remain Spreader/Pool blocks.

---

# Quick Reference

| Item | Produces / Holds | Crafted From |
| --- | --- | --- |
| Sunblaze Flower | up to ≈ 7.3 Mn/s | — |
| Springfroth Flower | ≈ 4.2 Mn/s | — |
| Mana Spreader | 1200 Mn | 7 Wood + Iron Ingot + Petal |
| Mana Pool | 3800 Mn | 5 Grown Stone |
| Grown Staff | — | 3 Wood + 2 Petal |
| Mana Ingot | −300 Mn from pool | Iron Ingot + pool Mana |

---

*Balance and tuning live in the code — see `Subsystems/` and the balanced numbers in `SunPowerBehavior`, `WaterDonBehavior`, `SubsystemMana` and `GrownStaffBehavior`.*