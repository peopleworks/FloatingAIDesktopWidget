# 🎨 Guía de Configuración de Iconos

## Cómo Agregar Iconos a los Satélites

Los satélites (widgets flotantes) pueden mostrar iconos personalizados para cada aplicación. Si no se especifica un icono, se mostrará la primera letra del nombre.

## Configuración en appsettings.json

```json
{
  "Targets": [
    {
      "Name": "ChatGPT",
      "IconPath": "icons/chatgpt.png",  // <-- Agregar aquí
      "FileName": "C:\\Path\\To\\ChatGPT.exe",
      ...
    }
  ]
}
```

## Formatos de Ruta Soportados

### 1. Ruta Relativa (recomendado)
```json
"IconPath": "icons/app.png"
```
- Se busca relativo al ejecutable (carpeta bin)
- Ideal para distribuir con la aplicación

### 2. Ruta Absoluta
```json
"IconPath": "C:\\Users\\Usuario\\Pictures\\Icons\\app.png"
```
- Ruta completa al archivo
- Útil para iconos en ubicaciones específicas

### 3. Variables de Entorno
```json
"IconPath": "%APPDATA%\\MyApp\\icons\\app.png"
"IconPath": "%USERPROFILE%\\Pictures\\app-icon.png"
```
- Usa variables de Windows
- Portable entre usuarios

## Formatos de Imagen Soportados

✅ **PNG** (recomendado - soporta transparencia)
✅ **JPG/JPEG**
✅ **ICO** (iconos de Windows)
✅ **BMP**

**Tamaño recomendado:** 24x24 px a 256x256 px (se escala automáticamente)

## Dónde Conseguir Iconos

### Fuentes Gratuitas:
- **Icons8**: https://icons8.com (PNG/ICO gratuitos)
- **Flaticon**: https://flaticon.com
- **IconArchive**: https://iconarchive.com
- **Windows Icons**: Extraer de aplicaciones instaladas

### Extraer Iconos de Aplicaciones

**Método 1: Copiar desde ejecutable**
1. Busca el .exe de la aplicación
2. Click derecho → Propiedades
3. Los iconos ya están en el .exe - usa la ruta del .exe como IconPath

**Método 2: Usar herramientas**
- ResourceHacker (extraer iconos de .exe)
- IcoFX (editor de iconos)

## Ejemplos Prácticos

### Ejemplo 1: Aplicaciones Populares

```json
{
  "Targets": [
    {
      "Name": "ChatGPT",
      "IconPath": "C:\\Users\\%USERNAME%\\AppData\\Local\\Programs\\chatgpt\\resources\\app.ico",
      "FileName": "C:\\Users\\%USERNAME%\\AppData\\Local\\Programs\\chatgpt\\ChatGPT.exe"
    },
    {
      "Name": "Visual Studio Code",
      "IconPath": "C:\\Program Files\\Microsoft VS Code\\Code.exe",
      "FileName": "C:\\Program Files\\Microsoft VS Code\\Code.exe"
    },
    {
      "Name": "Google Chrome",
      "IconPath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
      "FileName": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe"
    }
  ]
}
```

### Ejemplo 2: Iconos Personalizados en Carpeta Local

**Estructura de carpetas:**
```
FloatingAIDesktopWidget/
├── FloatingAIDesktopWidget.exe
└── icons/
    ├── chatgpt.png
    ├── claude.png
    └── copilot.png
```

**Configuración:**
```json
{
  "Targets": [
    {
      "Name": "ChatGPT",
      "IconPath": "icons/chatgpt.png",
      "FileName": "..."
    },
    {
      "Name": "Claude",
      "IconPath": "icons/claude.png",
      "FileName": "..."
    },
    {
      "Name": "Copilot",
      "IconPath": "icons/copilot.png",
      "FileName": "..."
    }
  ]
}
```

### Ejemplo 3: Sin Iconos (Letras Iniciales)

```json
{
  "Targets": [
    {
      "Name": "Notepad",
      "IconPath": "",  // <-- Vacío o sin especificar
      "FileName": "notepad.exe"
    }
  ]
}
```
Resultado: Mostrará la letra **"N"** en el satélite

## Creación de Carpeta de Iconos

**PowerShell:**
```powershell
# Crear carpeta icons en el directorio de la aplicación
New-Item -ItemType Directory -Path "icons" -Force

# Descargar icono de ejemplo (requiere internet)
Invoke-WebRequest -Uri "https://ejemplo.com/icon.png" -OutFile "icons/app.png"
```

**CMD:**
```cmd
mkdir icons
```

## Resolución de Problemas

### El icono no se muestra

**Verificaciones:**
1. ✅ La ruta del archivo es correcta
2. ✅ El archivo existe en la ubicación especificada
3. ✅ El formato de imagen es soportado (PNG, JPG, ICO, BMP)
4. ✅ El archivo no está corrupto
5. ✅ Tienes permisos de lectura en el archivo

**Probar:**
```json
// Cambia la ruta temporalmente a una absoluta para verificar
"IconPath": "C:\\Windows\\System32\\notepad.exe"
```

### Se muestra la letra en lugar del icono

Esto es normal si:
- `IconPath` está vacío o es `null`
- El archivo no existe en la ruta especificada
- Hubo un error al cargar la imagen

**Revisar logs:** La aplicación ignora errores silenciosamente, así que verifica la ruta manualmente.

## Consejos y Mejores Prácticas

### ✅ Recomendado:
- Usar PNG con transparencia para mejor apariencia
- Tamaño 64x64 px o 128x128 px
- Guardar iconos en carpeta "icons" junto al ejecutable
- Usar nombres descriptivos: `chatgpt.png`, `vscode.png`

### ❌ Evitar:
- Iconos muy grandes (>512x512 px) - consumen memoria
- Rutas con espacios sin comillas
- Rutas hardcoded del usuario (usa %USERPROFILE%)
- Formatos no soportados (SVG, WEBP, etc.)

## Ejemplo Completo

```json
{
  "_Comment": "Configuración de ejemplo con iconos",

  "Targets": [
    {
      "Name": "ChatGPT",
      "IconPath": "icons/chatgpt.png",
      "FileName": "C:\\Users\\%USERNAME%\\AppData\\Local\\Programs\\chatgpt\\ChatGPT.exe",
      "SingleInstance": true,
      "FocusExistingIfRunning": true
    },
    {
      "Name": "Claude Desktop",
      "IconPath": "icons/claude.png",
      "FileName": "C:\\Users\\%USERNAME%\\AppData\\Local\\Programs\\claude\\Claude.exe",
      "SingleInstance": true,
      "FocusExistingIfRunning": true
    },
    {
      "Name": "VS Code",
      "IconPath": "C:\\Program Files\\Microsoft VS Code\\Code.exe",
      "FileName": "C:\\Program Files\\Microsoft VS Code\\Code.exe",
      "SingleInstance": false
    },
    {
      "Name": "Notepad",
      "IconPath": "",
      "FileName": "notepad.exe",
      "SingleInstance": false
    }
  ],

  "UI": {
    "MultiTargetMode": "Satellites"
  }
}
```

## Recursos Adicionales

- **Convertir imágenes a PNG**: https://convertio.co
- **Redimensionar iconos**: https://resizeimage.net
- **Crear iconos desde logo**: https://favicon.io

---

¿Necesitas ayuda? Revisa el archivo `MULTI_TARGET_GUIDE.md` para más información sobre configuración general.
