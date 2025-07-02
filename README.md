[VRLearnToBot](https://github.com/LuisIgnaciodeLunaGomez/VRLearnTOBOT/blob/master/project-app/Assets/background/logo.png)

[![GitHub issues cerradas](https://img.shields.io/github/issues-closed/LuisIgnaciodeLunaGomez/VRLearnTOBOT)](https://github.com/LuisIgnaciodeLunaGomez/VRLearnTOBOT/issues)
[![Último commit](https://img.shields.io/github/last-commit/LuisIgnaciodeLunaGomez/VRLearnTOBOT)](https://github.com/LuisIgnaciodeLunaGomez/VRLearnTOBOT/commits)
[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Lenguaje principal](https://img.shields.io/github/languages/top/LuisIgnaciodeLunaGomez/VRLearnTOBOT)](https://github.com/LuisIgnaciodeLunaGomez/VRLearnTOBOT/)
[![Tamaño del código](https://img.shields.io/github/languages/code-size/LuisIgnaciodeLunaGomez/VRLearnTOBOT)](https://github.com/LuisIgnaciodeLunaGomez/VRLearnTOBOT/)
[![Zube](https://img.shields.io/badge/zube-managed-blue?logo=zube)](https://zube.io/)
![GitHub Release](https://img.shields.io/github/v/release/LuisIgnaciodeLunaGomez/VRLearnTOBOT?label=Release)

<h1>🚀 Laboratorio virtual  de Programación y Robótica (Scratch + Lego Wedo 2.0)</h1>

<h2><strong>Trabajo Fin de Grado curso 2024 -2025</strong>
</h2>
<h3>Ingeniería Informática Universidad de Burgos</h3>

Desarrollado por <strong>Luis Ignacio de Luna Gómez</strong>

🧑‍🏫 <strong>Tutor del trabajo</strong>
<p>Carlos López Nozal</p>
<p>Departamento de Ingeniería Informática, Universidad de Burgos</p>
<p>Contacto:  <a href="mailto:clopezno@ubu.es">clopezno@ubu.es</a></p>

 <h2>📌 Descripción del proyecto</h2>

**VRLearnTobot** es un prototipo de laboratorio virtual 3D desarrollado en **Unity** y **C#**, diseñado para enseñar los fundamentos de la programación algorítmica y la robótica a estudiantes en etapas preuniversitarias.

El proyecto implementa un innovador **sistema de programación híbrido**. A diferencia de las plataformas tradicionales, combina una estructura de programa basada en **bloques visuales** con una interfaz de **codificación textual** basada en un Lenguaje de Dominio Específico (DSL) propio. Este enfoque único sirve como un **puente pedagógico**, facilitando la transición de los estudiantes desde entornos puramente visuales como Scratch hacia la programación textual.

La plataforma presenta una serie de desafíos donde el usuario debe escribir o ensamblar secuencias de comandos para controlar un robot virtual, inspirado en la estética de kits como LEGO WeDo 2.0, en un entorno de simulación 3D interactivo. El objetivo final es ofrecer una herramienta educativa **accesible, didáctica y gratuita**, optimizada para su uso en tablets Android, y alineada con los Objetivos de Desarrollo Sostenible de democratizar la educación STEAM.

<h2>🎯  Características principales</h2>

-   🤖 **Simulador 3D interactivo:** Un robot virtual que responde en tiempo real a las instrucciones, en un entorno con detección de colisiones.
-   ✍️ **Lenguaje de dominio específico (DSL):** Un lenguaje de comandos simple y textual (`mover`, `girar`, `repetir`) diseñado para ser intuitivo para principiantes.
-   ⚙️ **Intérprete propio:** El proyecto incluye un parser que analiza el código del DSL, valida su sintaxis y lo traduce a una secuencia de instrucciones ejecutable.
-   🧱 **Programación híbrida:** Permite la programación mediante bloques visuales (eventos, bucles) o mediante la entrada textual de comandos.
-   🏆 **Aprendizaje basado en desafíos:** El contenido está organizado en desafíos con objetivos claros para contextualizar el aprendizaje y fomentar la resolución de problemas.
-   📱 **Orientado a plataformas móviles:** Diseñado y optimizado para ejecutarse en dispositivos Android, especialmente tablets, haciéndolo accesible para un mayor número de estudiantes.

<h2>🛠 Herramientas utilizadas:</h2>

-   **Motor de desarrollo:** Unity 6
-   **Lenguaje de programación:** C#
-   **Interfaz y lógica híbrida:**
    -   Base de programación por bloques adaptada desde **UBlockly**.
    -   Intérprete de DSL textual (**CommandParser**) de desarrollo propio.
-   **Modelado 3D:** Blender 4.4
-   **Control de versiones:** Git / GitHub
-   **Gestión de proyecto (Metodología ágil):** Zube

<h2>🚧 Estado del Proyecto</h2>

**Completado (Versión MVP para TFG)**. El proyecto ha culminado en un Producto Mínimo Viable (MVP) completamente funcional que demuestra la viabilidad técnica y pedagógica del enfoque híbrido. Las funcionalidades actuales incluyen la selección de desafíos, un entorno de programación híbrido, un intérprete de DSL funcional y una simulación 3D con detección de colisiones. Las futuras líneas de trabajo se centran en la expansión del catálogo de bloques visuales y la adición de nuevos comandos al DSL.


<h2>🚀 Cómo Empezar (Instrucciones Preliminares)</h2>

*Prerequisitos*

-   Unity hub
-   Unity editor **versión 6000.0.2f1** o superior.
-   Módulo "Android Build Support" instalado en Unity.
-   (Opcional) Un dispositivo Android con modo de depuración USB activado para pruebas.
-   Configuración de pantalla mímina para la correcdta ejecución del programa 1920x1080

*Instalacion y ejecucción*


1.  **Clonar el repositorio:**
    ```bash
    git clone https://github.com/LuisIgnaciodeLunaGomez/VRLearnTOBOT
    ```
2.  **Abrir en Unity:**
    -   Abrir Unity Hub y hacer clic en "Add project from disk".
    -   Seleccionar la carpeta raíz del repositorio clonado.
3.  **Ejecutar en el Editor:**
    -   Navegar a la carpeta `Assets/Scenes/` y abrir la escena `MenuScene.unity`.
    -   Presionar el botón "Play" en la barra de herramientas de Unity.

4.  **Compilar para Android (.apk):**
    -   Ir a `File > Build Settings`.
    -   Seleccionar "Android" como plataforma y pulsar "Switch Platform".
    -   Conectar un dispositivo Android o configurar un emulador.
    -   Pulsar "Build and Run" para compilar e instalar directamente en el dispositivo.

<h2>📬 <strong>Contacto</strong></h2

<ul>
<li><strong>Email: </strong><a href="mailto:correo@luisgnaciodeluna.com">correo@luisgnaciodeluna.com </a> | <a href="mailto:ldg1008@alu.ubu.es">ldg1008@alu.ubu.es</li>

 <li><strong>Web: </strong><a href="https://luisignaciodeluna.com">Luisignaciodeluna.com</a></li>
<li><strong>Linkedin: </strong><a href="https://www.linkedin.com/in/luisignaciodeluna/">Perfil</li>
 
</ul>

