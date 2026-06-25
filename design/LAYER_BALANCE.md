# Layer Balance & Simulation Tuning

**Version**: 0.1.0-alpha
**Last Updated**: June 25, 2026
**Location**: `design/LAYER_BALANCE.md`

> These are **starting** tuning values for the Phase 1 vertical slice. Treat them
> as data, not code — they should live in ScriptableObjects / config assets so
> designers can iterate without recompiling.

---

## 1. Tier Update Cadence

| Tier | Update Rate | Rationale |
|---|---|---|
| Strategic (Geoscape) | every 30–60 s sim-time | Background abstraction; cheap |
| Operational | 1–10 Hz | Squad pathing / objective refresh |
| Tactical (FPS) | per-frame (60+ Hz) | Player-facing fidelity |

---

## 2. Out-of-Sight (OOS) Battle Math

Each tick, a contested sector resolves a stochastic exchange:

```
defenseEffective = HumanDefensePower * terrainModifier
assaultEffective = VekariAssaultForce

// proportional attrition with randomized variance
defenseLoss = assaultEffective * k_attrition * rand(0.8, 1.2)
assaultLoss = defenseEffective * k_attrition * rand(0.8, 1.2)

HumanDefensePower -= defenseLoss
VekariAssaultForce -= assaultLoss

if HumanDefensePower <= 0:  owner = Vekari
if VekariAssaultForce <= 0: owner = Human
```

| Parameter | Default | Meaning |
|---|---:|---|
| `k_attrition` | 0.05 | Fraction of effective power traded per tick |
| `terrainModifier` | 0.8–1.5 | Defender bonus by terrain type |
| variance | ±20% | Randomized roll per exchange |

---

## 3. Withdrawal Sandbox (Enemy Arrival Rate)

Drives the Act I scripted disaster transition (Defend → Preserve Combat Power).

| Parameter | Default | Meaning |
|---|---:|---|
| `arrivalRatePerSec` | 4 | Vekari entities emerging from the rift |
| `arrivalAccel` | 1.08 | Multiplier applied per wave (escalation) |
| `lineHoldThreshold` | 0.35 | Friendly strength ratio that triggers retreat |
| `retreatCohesion` | 0.6 | How orderly the automated retreat stays |

Transition logic (data-driven, not timer-driven):

```
friendlyRatio = friendlyStrength / initialFriendlyStrength
if friendlyRatio < lineHoldThreshold:
    squadState = RETREAT          // switch from HOLD_LINE
    objective  = ProtectTransports
```

---

## 4. Probabilistic Damage Overlay

| Parameter | Default | Meaning |
|---|---:|---|
| `noiseScale` | 0.05 | Frequency of Perlin/Simplex carving |
| `craterThreshold` | 0.7 | Noise value above which voxels are carved to Air |
| `maxCarveDepth` | 6 | Max voxel depth of a single scar pass |
| `bombardmentTiers` | 0–3 | Severity flag set by the OOS sim |

Higher `bombardmentTiers` raises carve density during the loading pre-pass so a
heavily fought-over sector loads visibly scarred.

---

## 5. Tuning Workflow

1. Expose all values above as serialized config assets.
2. Change values in the editor; never hard-code balance in systems.
3. Record notable balance shifts in `RECENT_CHANGES.md`.
