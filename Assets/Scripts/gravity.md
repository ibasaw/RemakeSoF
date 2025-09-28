# CSGO Bunnyhop / Longjump Formeln

Dieses Dokument fasst alle relevanten Berechnungen für CSGO-ähnliches Bunnyhopping / Longjumps zusammen.

---

## 1️⃣ Grundgeschwindigkeit

```text
v_new = v_old + (wishDir * airAccel + mousePush) * deltaTime
```

* v\_old = aktuelle horizontale Velocity (X/Z)
* wishDir = normalized forward + minimal side (z.B. forward + 0.1 \* strafeDir)
* airAccel = Luftbeschleunigung (höher → mehr Speed pro Frame)
* mousePush = Maus-X-Delta \* mouseStrafeMultiplier \* mouseAccelFactor
* deltaTime = FixedDeltaTime
* Maus-Push wirkt nur, wenn Mausrichtung und A/D Input synchron sind

---

## 2️⃣ Side-Speed → Forward-Speed Konvertierung

```text
v_forward += abs(dot(v, strafeDir)) * k
v_side -= dot(v, strafeDir) * k
```

* k \~ 0.8 (80% Seitwärtsgeschwindigkeit → Vorwärts)
* strafeDir = A/D Richtung (normalized)
* Nur anwenden, wenn |dot(v, strafeDir)| > epsilon

---

## 3️⃣ Air-Speed Cap

```text
if (v_new.magnitude > airSpeedCap)
    v_new = v_new.normalized * airSpeedCap
```

* AirSpeedCap begrenzt horizontale Geschwindigkeit

---

## 4️⃣ Forward Boost beim Strafing

```text
strafeSpeed = dot(v, strafeDir)
v_forward += strafeSpeed * 0.8
```

* Effekt: mehr horizontaler Speed abhängig von Side-Strafe

---

## 5️⃣ Landing Bonus (optional)

```text
landingSpeedBonus = lastHorizontalVelocity.magnitude * 0.01
v_ground = v_horizontal + landingSpeedBonus
landingSpeedBonus *= landingBonusDecay
```

* landingSpeedBonus \~ 1-5% der Horizontalgeschwindigkeit
* Exponentiell nach Landung reduziert

---

## 6️⃣ Jump-Formel

```text
v_y = sqrt(2 * g * jumpHeight)
t_air = 2 * v_y / g
```

* jumpHeight = Sprunghöhe
* g = |Gravity|
* Delta x = v\_horizontal \* t\_air → Distance

---

## 7️⃣ Distance Gain / Longjump

```text
d_jump = v_horizontal * t_air
```

* horizontal = aktuelle Velocity nach AirAccel + MousePush + ForwardBoost
* Höhere AirAccel → höhere horizontal velocity → längere Distanz
* Höhere jumpHeight → längere AirTime → mehr Distance

---

## 8️⃣ Strafe-Efficiency (Debug / GUI)

```text
efficiency = 1 - Angle(v_horizontal, wishDir) / 90°
```

* 0 → völlig seitlich
* 1 → perfekt forward

---

## 9️⃣ Maus / Input-Sync Bedingung

```text
if (sign(A/D) == sign(MouseDeltaX))
    apply full AirAccel
else
    minimal AirAccel
```

* Nur SpeedGain, wenn A/D Input und Maus synchron

---

## 🔧 Praktische Parameter für CSGO-Style

| Parameter             | Typische Werte |
| --------------------- | -------------- |
| airAccel              | 50-100         |
| mouseStrafeMultiplier | 0.002          |
| mouseAccelFactor      | 30             |
| velAlignFactor        | 2              |
| airSpeedCap           | 300            |
| jumpHeight            | 1.5 - 1.8      |
| sideBoostFactor       | 0.8            |

---

Diese Formeln kannst du direkt in Unity implementieren, um Bhop/Longjump-Mechaniken ähnlich wie in CSGO zu simulieren.


