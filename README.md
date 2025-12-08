# 🎯 Cliente Tarkov

> Aplicación de escritorio dedicada para Tarkov Market Pilot

**Modificación del Cliente Tarkov v0.1.5 basado en el proyecto original de TarkovClient.**

El Cliente Tarkov es un programa que ejecuta el sitio web Tarkov Market Pilot como una aplicación de escritorio dedicada.
Se integra con el juego para proporcionar detección de mapas en tiempo real, seguimiento de ubicación y limpieza automática de archivos.
El sitio web Tarkov Market se cachea en la carpeta "Cache" para que el programa pueda funcionar sin conexión.

## ⚡ Características Principales

- ✅ **Distribución Autocontenida** - No requiere instalación separada de .NET Runtime
- ✅ **Ejecutable Único** - Ejecución inmediata sin procesos de instalación complejos
- ✅ **Soporte de Múltiples Pestañas** - Uso simultáneo de varias páginas web
- ✅ **Detección de Mapa en Tiempo Real** - Detección automática de cambio de mapa basada en registros del juego
- ✅ **Rastreo de Capturas de Pantalla** - Rastreo de ubicación y dirección basado en capturas de pantalla del juego
- ✅ **Limpieza Automática de Archivos** - Limpieza automática de carpetas de registro y capturas de pantalla (optimización de rendimiento)
- ✅ **Optimización de Procesamiento Paralelo** - Rendimiento mejorado en el procesamiento de archivos
- ✅ **Cacheo de Mapas Local** - Funcionamiento sin conexión gracias al cacheo local

## 📥 Descargar

_Última versión_: [Latest Release](../../releases/latest)

**Cómo instalar**:

1. Descarga el archivo ZIP
2. Descomprime
3. Haz doble clic en `TarkovClient.exe`

**Ventajas**: Ejecución inmediata sin instalación, portable

## 🖥️ Requisitos del Sistema

- **Sistema Operativo**: Windows 10/11 (64 bits)
- **Memoria**: Mínimo 512MB de espacio libre
- **Otros**: WebView2 Runtime (Incluido por defecto en Windows 11)

> ⚠️ **WebView2 Runtime**: Los usuarios de Windows 10 deben **actualizar Windows a la última versión** para la instalación automática. Si el problema persiste después de la actualización, instala manualmente desde [Descarga de Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/).

## 🚀 Uso

### Ejecución Básica

1. **Iniciar Programa**

   - Haz doble clic en `TarkovClient.exe`
   - Carga automáticamente la página de Tarkov Market Pilot
   - Aparece un icono en la bandeja del sistema

2. **Menú de la Bandeja del Sistema**
   - **Open**: Abrir el sitio web en el navegador predeterminado
   - **Exit**: Cerrar el programa completamente

### Uso de Pestañas

- **Añadir nueva pestaña**: Haz clic en el botón `+` arriba a la izquierda
- **Cerrar pestaña**: Haz clic en el botón `✕` de cada pestaña
- **Mínimo 1 pestaña** siempre se mantiene

### Configuración de Integración con el Juego

#### Detección Automática (Recomendado)

El programa busca automáticamente la ruta de instalación del juego:

1. **Carpeta del Juego**: Búsqueda automática en el registro de Windows
2. **Carpeta de Capturas**: Búsqueda automática en la carpeta de documentos del usuario

#### Configuración Manual

Configuración manual en la interfaz web si falla la detección automática:

1. **Ruta de la Carpeta del Juego**

   - Valor predeterminado: `C:\Battlestate Games\Escape from Tarkov\`
   - Ejemplo: `D:\Games\Escape from Tarkov\`

2. **Ruta de la Carpeta de Capturas**
   - Valor predeterminado: `%USERPROFILE%\Documents\Escape From Tarkov\Screenshots\`
   - Ejemplo: `C:\Users\NombreUsuario\Documents\Escape From Tarkov\Screenshots\`

### Configuración de Rastreo de Capturas

El rastreo de ubicación comienza automáticamente cuando tomas una captura de pantalla dentro del juego. Usa la tecla de captura de pantalla configurada en el juego.

## 🔧 Funciones Principales

### 🗺️ Detección Automática de Mapa

- Monitoreo en tiempo real del archivo de registro del juego
- Detección y visualización automática al cambiar de mapa
- Indicador de dirección para mostrar hacia dónde se mira

### 📸 Rastreo de Ubicación por Captura

- Análisis automático de capturas de pantalla del juego
- Actualización de ubicación y dirección en tiempo real
- Rastreo del progreso de misiones

### 🧹 Sistema de Limpieza Automática de Archivos

**¡Actualizado para optimización de rendimiento!**

#### Limpieza de Carpeta de Registros

- Limpieza automática de carpetas de registro antiguas al iniciar el programa
- Conserva solo la carpeta más reciente, elimina el resto
- Ahorro de espacio en disco y mejora de rendimiento

#### Limpieza Automática de Capturas

- Ejecución automática al inicializar BattlEye
- Rendimiento de eliminación rápido con **procesamiento paralelo**
- Resolución automática de problemas de permisos de archivos
- Optimización de recursos del sistema

### 🌐 Servidor WebSocket

- Puerto: `localhost:5123`
- Comunicación en tiempo real con la interfaz web
- Transmisión de datos del juego en tiempo real

## 🛠️ Solución de Problemas

### Advertencia de Windows Defender

**Causa**: No se posee certificado de firma de código (de pago)
**Solución**:

1. En la ventana de advertencia de Windows Defender, haz clic en **"Más información"**
2. Haz clic en el botón **"Ejecutar de todas formas"**
3. Procede con la instalación normal

> 💡 **Seguridad**: Proyecto de código abierto con todo el código público para garantizar transparencia.

### Errores relacionados con WebView2

- **Windows 11**: Incluido por defecto, sin problemas
- **Windows 10**: Reintentar después de ejecutar Windows Update
- **Instalación Manual**: [Descargar WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)

### Conflicto de Puerto (5123)

**Síntoma**: Fallo al iniciar el servidor WebSocket
**Solución**: Cerrar otros programas y reiniciar

### Si no se detecta el mapa

**Causa**: Fallo de acceso al archivo de registro o problema de permisos

**Solución**:

1. **Ejecutar como administrador**
   - Clic derecho en `TarkovClient.exe` → **Ejecutar como administrador**
2. **Verificar ruta de carpeta del juego**
   - Verificar en la interfaz web si la ruta es correcta
   - Valor predeterminado: `C:\Battlestate Games\Escape from Tarkov\`
3. **Verificar permisos de carpeta de registros**
   - Verificar permisos de lectura en la carpeta `CarpetaJuego\Logs`

### Si no funciona el rastreo por captura

**Causa**: Problema de ruta de carpeta de capturas o configuración de teclas

**Solución**:

1. **Verificar ruta de carpeta de capturas**
   - Valor predeterminado: `%USERPROFILE%\Documents\Escape From Tarkov\Screenshots\`
2. **Probar función de captura**
   - Tomar una captura en el juego y verificar si se crea el archivo

### Si la limpieza automática no funciona

**Causa**: Problema de permisos de archivo o fallo de acceso a carpeta

**Solución**:

1. **Ejecutar como administrador**
   - Puede requerir permisos de eliminación de archivos
2. **Verificar permisos de carpeta**
   - Verificar permisos de escritura en carpetas de registro y capturas
3. **Limpieza manual**
   - Limpiar manualmente las carpetas si es necesario y reiniciar el programa

### Si aparece advertencia de firewall

**Solución**:

- Seleccionar permitir en el Firewall de Windows Defender
- Verificar uso del puerto 5123
- Registrar el programa como aplicación de confianza

## 🏗️ Información de Desarrollo

### Stack Tecnológico

- **.NET 8.0** - Despliegue Autocontenido
- **WPF** - UI Nativa de Windows
- **WebView2** - Renderizado web basado en Chromium
- **Fleck** - Librería de servidor WebSocket

### Comandos de Construcción

```bash
# Build de desarrollo
./main.ps1 dev

# Build de publicación Autocontenida
./main.ps1 publish

# Paquete ZIP para GitHub Release
./main.ps1 package
```

## 🔒 Seguridad y Privacidad

- ✅ **Ejecución Local**: Todo el procesamiento se realiza localmente
- ✅ **Solo Lectura**: No modifica los archivos del juego
- ✅ **Privacidad**: No recopila información personal
- ✅ **Comunicación Segura**: Solo se comunica con Tarkov Market

## 📝 Actualización

1. Cerrar el programa existente
2. Descargar el nuevo archivo ZIP
3. Descomprimir y sobrescribir
4. Reiniciar el programa

> 💾 **Conservación de Configuración**: Todas las configuraciones de usuario se conservan automáticamente.

## 🆘 Soporte y Consultas

**En caso de problemas**:

1. Intentar ejecutar como administrador
2. Consultar en [GitHub Issues](../../issues)

## 🔗 Enlaces

- **Repositorio GitHub**: [TarkovClient](../../)
- **Reporte de Problemas**: [GitHub Issues](../../issues)
- **Últimas Versiones**: [Releases](../../releases)
- **Tarkov Market**: [https://tarkov-market.com/pilot](https://tarkov-market.com/pilot)

---

<div align="center">

**Tarkov Client v0.1.5**  
© 2025 TarkovClient Project

[GitHub](../../) • [Issues](../../issues) • [Releases](../../releases)

</div>
