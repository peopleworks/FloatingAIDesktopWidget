# 🎯 Guía Multi-Target

## Descripción

El FloatingAIDesktopWidget ahora soporta **múltiples aplicaciones/chats** desde un solo widget flotante. El comportamiento se adapta automáticamente según el número de targets configurados.

## Comportamiento Adaptativo

### 1 Target Configurado
- **Click izquierdo**: Lanza la aplicación directamente
- **Click derecho → Open assistant**: Lanza la aplicación directamente
- **Click derecho → Close assistant**: Cierra la aplicación directamente
- **Hotkey global**: Lanza la aplicación directamente

### 2+ Targets Configurados
- **Click izquierdo**: Muestra menú de selección con todas las aplicaciones
- **Click derecho → Select application...**: Muestra menú de selección
- **Click derecho → Select application... (Close)**: Muestra submenú para elegir cuál cerrar
- **Hotkey global**: Muestra menú de selección

## Configuración

### Formato Nuevo (Multi-Target)

```json
{
  "Targets": [
    {
      "Name": "ChatGPT",
      "IconPath": "icons/chatgpt.png",
      "FileName": "C:\\Program Files\\ChatGPT\\ChatGPT.exe",
      "Arguments": "",
      "WorkingDirectory": "",
      "RunAsAdministrator": false,
      "SingleInstance": true,
      "FocusExistingIfRunning": true,
      "AllowCloseFromMenu": true
    },
    {
      "Name": "Claude Desktop",
      "IconPath": "icons/claude.png",
      "FileName": "C:\\Users\\%USERNAME%\\AppData\\Local\\Programs\\claude\\Claude.exe",
      "Arguments": "",
      "WorkingDirectory": "",
      "RunAsAdministrator": false,
      "SingleInstance": true,
      "FocusExistingIfRunning": true,
      "AllowCloseFromMenu": true
    }
  ],
  "UI": {
    "MultiTargetMode": "ContextMenu"
  }
}
```

### Formato Legacy (Compatible)

El formato antiguo sigue funcionando para compatibilidad hacia atrás:

```json
{
  "Target": {
    "FileName": "C:\\Path\\To\\App.exe",
    "Arguments": "",
    "SingleInstance": true
  }
}
```

La configuración legacy se convierte automáticamente al formato nuevo al cargar.

## Nuevas Propiedades

### TargetSettings

- **`Name`** (string, opcional): Nombre que aparece en el menú de selección
  - Si no se especifica, se genera automáticamente: "Asistente 1", "Asistente 2", etc.
  - Ejemplo: `"ChatGPT"`, `"Claude Desktop"`

- **`IconPath`** (string, opcional): Ruta al icono del target
  - Puede ser absoluta o relativa al directorio de la aplicación
  - Soporta variables de entorno: `"%APPDATA%\\icons\\app.png"`
  - Formatos: PNG, JPG, ICO, BMP
  - Se muestra en el menú de selección junto al nombre

### UiSettings

- **`MultiTargetMode`** (string): Modo de UI para múltiples targets
  - `"ContextMenu"`: Menú contextual nativo de Windows (implementado)
  - `"RadialCustom"`: Menú radial personalizado (futuro)
  - `"Satellites"`: Widgets satélite flotantes (futuro)

## Menú de Cierre

Cuando hay múltiples targets, el menú "Close assistant" muestra:

1. **Lista de aplicaciones abiertas**: Cada target que está ejecutándose
2. **Separador**
3. **"Close all assistants"**: Cierra todas las aplicaciones configuradas que estén abiertas

Los targets con `AllowCloseFromMenu: false` no aparecen en el menú de cierre.

## Migración Automática

El sistema detecta y migra automáticamente la configuración legacy:

1. Al cargar `appsettings.json`, detecta si usa formato antiguo (`Target`) o nuevo (`Targets[]`)
2. Si es formato antiguo, lo convierte automáticamente a array con 1 elemento
3. Si falta el campo `Name`, genera uno por defecto
4. No modifica el archivo original - la migración es solo en memoria

## Ejemplos de Uso

### Ejemplo 1: Configuración Simple (1 Target)

```json
{
  "Targets": [
    {
      "Name": "Mi Asistente",
      "FileName": "C:\\Path\\To\\Assistant.exe"
    }
  ]
}
```

**Resultado**: Click directo lanza la aplicación (sin menú)

### Ejemplo 2: Múltiples Chats

```json
{
  "Targets": [
    {
      "Name": "ChatGPT",
      "IconPath": "icons/chatgpt.png",
      "FileName": "C:\\Program Files\\ChatGPT\\ChatGPT.exe"
    },
    {
      "Name": "Claude",
      "IconPath": "icons/claude.png",
      "FileName": "C:\\Program Files\\Claude\\Claude.exe"
    },
    {
      "Name": "Copilot",
      "IconPath": "icons/copilot.png",
      "FileName": "microsoft-edge:https://copilot.microsoft.com"
    }
  ]
}
```

**Resultado**: Click muestra menú con 3 opciones, cada una con su icono

### Ejemplo 3: Sin Iconos (Solo Texto)

```json
{
  "Targets": [
    {
      "Name": "ChatGPT",
      "FileName": "C:\\Program Files\\ChatGPT\\ChatGPT.exe"
    },
    {
      "Name": "Claude Desktop",
      "FileName": "C:\\Program Files\\Claude\\Claude.exe"
    }
  ]
}
```

**Resultado**: Menú de texto sin iconos

## Pruebas

### Test 1: Compatibilidad Legacy
1. Mantén el `appsettings.json` actual con formato `Target` (singular)
2. Ejecuta la aplicación
3. ✅ Debe funcionar exactamente como antes (click directo)

### Test 2: Multi-Target
1. Copia `appsettings.multi-target-example.json` → `appsettings.json`
2. Ajusta las rutas de las aplicaciones
3. Ejecuta la aplicación
4. ✅ Click izquierdo debe mostrar menú de selección

### Test 3: Reload en Caliente
1. Con la app ejecutándose, edita `appsettings.json`
2. Cambia de 1 target a múltiples (o viceversa)
3. ✅ La aplicación debe recargar automáticamente sin reiniciar

### Test 4: Menú de Cierre
1. Configura múltiples targets
2. Lanza algunas aplicaciones desde el widget
3. Click derecho → Select application... (en "Close")
4. ✅ Debe mostrar lista de apps abiertas + "Close all"

## Futuras Opciones de UI

Las siguientes opciones están planificadas para implementarse después:

### Opción B: Radial Menu Personalizado
- Menú circular dibujado con GDI+
- Similar a DevExpress pero sin dependencias
- Configurar con: `"MultiTargetMode": "RadialCustom"`

### Opción C: Widgets Satélite
- Botones secundarios que aparecen alrededor del widget principal
- Animaciones suaves
- Configurar con: `"MultiTargetMode": "Satellites"`

## Soporte

- **Archivo de configuración**: `%APPDATA%\FloatingAIDesktopWidget\appsettings.json`
- **Ejemplo multi-target**: `appsettings.multi-target-example.json`
- **Localización**: Español e Inglés (automático según sistema)

## Notas Técnicas

- **Compatibilidad**: Windows 10/11
- **.NET**: Requiere .NET 9.0 Runtime
- **Hot Reload**: Los cambios en `appsettings.json` se aplican automáticamente
- **Performance**: El menú de selección se construye dinámicamente al hacer click
- **Iconos**: Se cargan bajo demanda para minimizar uso de memoria
