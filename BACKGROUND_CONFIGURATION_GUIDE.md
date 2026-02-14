# 🎨 Guía de Configuración del Fondo del Botón Flotante

## 📌 Problema Resuelto

Si su ícono PNG tiene **transparencia** y se ve con un fondo negro feo, esta nueva configuración le permite:
- ✅ Detección automática de transparencia (fondo blanco automático)
- ✅ Elegir colores personalizados
- ✅ Varios estilos predefinidos

---

## 🚀 Inicio Rápido

### Para Íconos con Transparencia (Recomendado)

Edite `appsettings.json`:

```json
{
  "UI": {
    "IconPath": "mi-icono-transparente.png",
    "BackgroundStyle": "auto"
  }
}
```

**Resultado**: El fondo será automáticamente **blanco/claro** ✅

---

## 🎨 Estilos Disponibles

### 1. **`"auto"` - Detección Automática (Por Defecto)**

```json
{
  "UI": {
    "BackgroundStyle": "auto"
  }
}
```

**Comportamiento**:
- Si el PNG tiene transparencia → fondo **blanco/claro**
- Si el PNG NO tiene transparencia → fondo **oscuro** (original)

---

### 2. **`"light"` - Fondo Claro**

```json
{
  "UI": {
    "BackgroundStyle": "light"
  }
}
```

**Uso**: Íconos oscuros con transparencia

**Colores**: Blanco (#F0F0F5) → Gris claro (#DCDCE6)

---

### 3. **`"dark"` - Fondo Oscuro**

```json
{
  "UI": {
    "BackgroundStyle": "dark"
  }
}
```

**Uso**: Íconos claros (comportamiento original)

**Colores**: Gris oscuro (#3C3C4B) → Casi negro (#23232D)

---

### 4. **`"transparent"` - Fondo Mínimo**

```json
{
  "UI": {
    "BackgroundStyle": "transparent"
  }
}
```

**Uso**: Estilo minimalista, casi sin fondo

**Efecto**: Solo un leve halo translúcido

---

### 5. **`"custom"` - Colores Personalizados**

```json
{
  "UI": {
    "BackgroundStyle": "custom",
    "BackgroundColorTop": "#0066CC",
    "BackgroundColorBottom": "#004C99"
  }
}
```

**Uso**: Colores de marca/empresa

**Formato de Colores**:
- **6 dígitos**: `#RRGGBB` (ejemplo: `#FF0000` = rojo)
- **8 dígitos**: `#AARRGGBB` (ejemplo: `#80FF0000` = rojo semi-transparente)

---

## 📖 Ejemplos Prácticos

### Ejemplo 1: Logo Corporativo con Transparencia

**Problema**: Logo PNG transparente se ve horrible con fondo negro

**Solución**:
```json
{
  "UI": {
    "IconPath": "logo-empresa.png",
    "BackgroundStyle": "auto"
  }
}
```

✅ **Resultado**: Fondo blanco automático

---

### Ejemplo 2: Botón con Colores de Marca

**Objetivo**: Botón con colores azul corporativo

**Solución**:
```json
{
  "UI": {
    "IconPath": "icono-app.png",
    "BackgroundStyle": "custom",
    "BackgroundColorTop": "#1E90FF",
    "BackgroundColorBottom": "#1C86EE"
  }
}
```

✅ **Resultado**: Gradiente azul profesional

---

### Ejemplo 3: Fondo Verde Empresarial

**Objetivo**: Fondo verde claro (estilo eco/salud)

**Solución**:
```json
{
  "UI": {
    "BackgroundStyle": "custom",
    "BackgroundColorTop": "#E8F5E9",
    "BackgroundColorBottom": "#C8E6C9"
  }
}
```

✅ **Resultado**: Gradiente verde suave

---

### Ejemplo 4: Botón Rojo (Urgencia)

**Objetivo**: Botón de emergencia/urgencia

**Solución**:
```json
{
  "UI": {
    "BackgroundStyle": "custom",
    "BackgroundColorTop": "#FF5252",
    "BackgroundColorBottom": "#D32F2F"
  }
}
```

✅ **Resultado**: Gradiente rojo llamativo

---

### Ejemplo 5: Minimalista (Sin Fondo)

**Objetivo**: Solo el ícono, sin fondo visible

**Solución**:
```json
{
  "UI": {
    "BackgroundStyle": "transparent"
  }
}
```

✅ **Resultado**: Fondo casi invisible

---

## 🔧 Configuración Completa (Ejemplo)

```json
{
  "Target": {
    "FileName": "C:\\MiApp\\app.exe",
    "Arguments": "",
    "WorkingDirectory": "",
    "RunAsAdministrator": false,
    "SingleInstance": true,
    "FocusExistingIfRunning": true,
    "AllowCloseFromMenu": true
  },
  "UI": {
    "Size": 64,
    "Margin": 16,
    "AlwaysOnTop": true,
    "ShowInTaskbar": false,
    "SnapToEdge": true,
    "Opacity": 1.0,
    "IconPath": "logo-empresa.png",
    "Language": "auto",

    "BackgroundStyle": "auto",
    "BackgroundColorTop": "#1E90FF",
    "BackgroundColorBottom": "#1C86EE"
  },
  "Hotkey": {
    "Enabled": true,
    "Gesture": "Ctrl+Shift+Space"
  }
}
```

---

## 🎨 Selector de Colores

Use estas herramientas online para elegir colores:

1. **Google Color Picker**: https://g.co/kgs/colorpicker
2. **Coolors**: https://coolors.co/
3. **Adobe Color**: https://color.adobe.com/

**Tip**: Copie el código hex (ejemplo: `#1E90FF`) y péguelo en la configuración.

---

## 💡 Recomendaciones

| Tipo de Ícono | Estilo Recomendado | Por Qué |
|---------------|-------------------|---------|
| PNG con transparencia | `"auto"` o `"light"` | Evita fondo negro feo |
| Logo corporativo | `"custom"` | Usa colores de marca |
| Ícono oscuro | `"light"` | Buen contraste |
| Ícono claro | `"dark"` | Buen contraste |
| Estilo minimalista | `"transparent"` | Discreto y moderno |

---

## 🔄 Cómo Aplicar Cambios

1. **Edite** `appsettings.json` con los cambios deseados
2. **Guarde** el archivo
3. **Reinicie** la aplicación flotante

**O** use el menú contextual:
- Click derecho en el botón flotante
- Seleccione **"Reload Settings"**

---

## ❓ Preguntas Frecuentes

### P: ¿Cómo sé si mi PNG tiene transparencia?

**R**: Ábralo en Windows Paint. Si ve un fondo de cuadros grises/blancos, tiene transparencia.

---

### P: ¿Qué pasa si uso "auto" con un PNG sin transparencia?

**R**: Usará el fondo **oscuro** original (comportamiento por defecto).

---

### P: ¿Puedo usar transparencia en colores personalizados?

**R**: ¡Sí! Use formato de 8 dígitos: `#AARRGGBB`

Ejemplo semi-transparente:
```json
{
  "BackgroundColorTop": "#80FFFFFF",
  "BackgroundColorBottom": "#60E0E0E0"
}
```

---

### P: No me gusta ningún estilo, ¿qué hago?

**R**: Use `"custom"` y experimente con diferentes colores hasta encontrar el que le guste.

---

### P: ¿Los cambios requieren recompilar?

**R**: **No**. Solo edite `appsettings.json` y recargue la configuración.

---

## 🆘 Solución de Problemas

### Problema: El fondo sigue negro

**Causa**: Usando estilo `"dark"` o `"auto"` sin transparencia detectada

**Solución**: Cambie a `"light"` o `"auto"`:
```json
{ "BackgroundStyle": "light" }
```

---

### Problema: El color personalizado no se aplica

**Causa 1**: Formato de color incorrecto

**Solución**: Use formato hex correcto (`#RRGGBB` o `#AARRGGBB`)

**Causa 2**: Falta `BackgroundStyle: "custom"`

**Solución**:
```json
{
  "BackgroundStyle": "custom",
  "BackgroundColorTop": "#...",
  "BackgroundColorBottom": "#..."
}
```

---

### Problema: El botón se ve raro al hacer hover

**Causa**: Los colores se ajustan automáticamente para hover/press

**Solución**: Use colores más saturados/oscuros. El sistema los aclara automáticamente en hover.

---

## 📞 Soporte

Si tiene problemas:

1. Verifique que `appsettings.json` tenga formato JSON válido
2. Revise los logs de la aplicación
3. Intente con `"BackgroundStyle": "light"` primero (es el más seguro)

---

## 🔄 Changelog

- **v1.1.0**: Agregado soporte para fondos configurables
  - Detección automática de transparencia
  - Estilos: auto, light, dark, transparent, custom
  - Colores personalizados en formato hex

---

**Última actualización**: 2026-02-12

**Compatibilidad**: FloatingAIDesktopWidget v1.1.0+
