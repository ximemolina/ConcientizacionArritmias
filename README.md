# 💓 Las señales del corazón — Experiencia VR

![Platform](https://img.shields.io/badge/Platform-Meta%20Quest-blue) ![Engine](https://img.shields.io/badge/Engine-Unity-black) ![XR](https://img.shields.io/badge/SDK-Meta%20XR%20SDK-informational)

Una experiencia de realidad virtual educativa e inmersiva diseñada para concientizar a los usuarios sobre las arritmias cardíacas, sus causas y sus síntomas, a través de una narrativa guiada y tres minijuegos interactivos.

---

## 📖 Descripción General

El usuario inicia en un entorno de descanso tranquilo que progresivamente se transforma en algo perturbador, representando el impacto de los malos hábitos en la salud cardiovascular. Una voz interior guía al usuario a través de la experiencia, llevándolo a comprender qué es una arritmia, qué hábitos la provocan y cómo reconocer sus síntomas.

La experiencia combina narración ambiental, diseño de sonido reactivo y mecánicas de juego hands-on para generar aprendizaje activo y emocional.

---

## 🛠️ Stack Técnico

| Componente | Detalle |
|-----------|---------|
| Motor | Unity 6.3 LTS |
| Template | VR Core |
| Plataforma objetivo | Meta Quest |
| Build target | Android (APK) |
| Input / Interacción | XR Interaction Toolkit |
| Simulación en PC | XR Device Simulator |

---

## 📸 Capturas de la Experiencia

### Bienvenida — Habitación de Descanso
![Habitación de descanso](docs/images/intro.png)

### Tutorial de Controles
![Tutorial de controles](docs/images/tutorial.png)

### Minijuego 1 — Estabilizá tu Corazón
![Minijuego 1](docs/images/minijuego1.png)

### Minijuego 2 — Agarrá Buenos Hábitos
![Minijuego 2](docs/images/minijuego2.png)

### Minijuego 3 — Cuarto de los Síntomas
![Minijuego 3](docs/images/minijuego3.png)

### Cierre
![Cierre](docs/images/cierre.png)

---

## 🗺️ Flujo de la Experiencia

```
Bienvenida (Habitación de descanso)
        ↓
  Tutorial de Controles
        ↓
  Portal → Minijuego 1
  [Estabilizá tu Corazón — Arritmias]
        ↓
  Portal → Minijuego 2
  [Agarrá Buenos Hábitos — Causas]
        ↓
  Portal → Minijuego 3
  [Cuarto de los Síntomas — Síntomas]
        ↓
     Cierre
  (Regreso a la habitación)
```

---

## 🎮 Controles

| Acción | Control |
|--------|---------|
| Girar cámara | Mover físicamente la cabeza |
| Seleccionar / Agarrar objetos | Botones frontales (cualquier control) |
| Navegar menús | Botones frontales |

---

## 🧩 Resumen de Minijuegos

| # | Nombre | Tema | Duración | Mecánica principal |
|---|--------|------|----------|--------------------|
| 1 | Estabilizá tu Corazón | Arritmias | Sin límite | Puzzle de piezas |
| 2 | Agarrá Buenos Hábitos | Causas / Hábitos | 1 minuto | Clasificación por agarre |
| 3 | Cuarto de los Síntomas | Síntomas | Sin límite | Clasificación de cuadros |

---

## 📄 Licencias y Créditos de Assets
Los modelos 3D, texturas y assets de terceros utilizados en esta experiencia están documentados en [`docs/CREDITS.md`](docs/CREDITS.md).

---
## 👥 Equipo


| Rol | Nombre |
|-----|--------|
| Diseño de experiencia |Laura Benavides Gamboa, Amanda Coto Robles, Melina Gálvez Navarro |
| Programación VR |Susana Feng Liu, Ximena Molina Portilla |

---