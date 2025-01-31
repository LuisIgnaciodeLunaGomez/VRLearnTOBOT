''' Trabajo fin de grado 2024-2025 - VRLearnTOBOT
 *
 * Grado en Ingeniería Informática - Universidad de Burgos
 *
 * Autor: Luis Ignacio de Luna Gómez
 * 
 * email: ldg1008@alu.ubu.es
 * 
 * Fecha: 21/01/2025
 * 
 * Versión: 1.0.0
 * 
 * Descripción: Uso de IA para detectar la ubicación del rectangulo de un bloque que es lo que modifica su tamaño según el contenido
'''

import cv2
import numpy as np
import os
import json

#  Ruta de la carpeta donde están las imágenes de los bloques
IMAGES_FOLDER = "../Assets/Resources/Icons/Textures"
OUTPUT_JSON = "../Assets/Resources/block_shapes.json"

# Definir patrones de nombres para clasificar los bloques
SPRITE_TYPES = {
    "Hat": "Hat_block_grey",
    "Stack": "Stack_block_grey",
    "Boolean": "Boolean_block_grey",
    "C": "C_block_grey",
    "Reporter": "Reporter_block_grey",
    "Cap": "Cap_block_grey"
}
#  Diccionario para almacenar la información de los bloques
block_data = {}

def detect_sprite_type(block_name):
    """ Intenta asignar un 'spriteName' basado en el nombre del archivo """
    for key, sprite_name in SPRITE_TYPES.items():
        if key.lower() in block_name.lower():
            return sprite_name
    return "Unknown_block"  # Si no coincide con nada, asignamos un valor por defecto

def analyze_block(image_path):
    """ Analiza una imagen de bloque y detecta el área rectangular central. """
    block_name = os.path.basename(image_path).split('.')[0]  # Nombre del archivo sin extensión
    image = cv2.imread(image_path, cv2.IMREAD_UNCHANGED)  # Cargar imagen con transparencia
    
    if image is None:
        print(f" No se pudo cargar {image_path}")
        return None
    
    height, width = image.shape[:2]

    #  Convertir la imagen a escala de grises y detectar bordes
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    edges = cv2.Canny(gray, 30, 150)  # Detección de bordes
    
    # Encontrar los contornos
    contours, _ = cv2.findContours(edges, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    
    #  Detectar el rectángulo central
    rect_x, rect_y, rect_w, rect_h = 0, 0, width, height  # Valores por defecto
    
    for contour in contours:
        x, y, w, h = cv2.boundingRect(contour)  # Encuentra el rectángulo de cada contorno
        
        # Seleccionamos el rectángulo más grande, que debería ser el principal
        if h > 20 and w > 20:  # Evitamos detectar ruido
            rect_x, rect_y, rect_w, rect_h = x, y, w, h
            break  # Tomamos el primer gran rectángulo encontrado

           # Detectar el tipo de sprite asociado
    sprite_name = detect_sprite_type(block_name)
    
    #  Guardar la información en el diccionario
    block_data[block_name] = {
        "width": width,
        "height": height,
        "rect_x": rect_x,
        "rect_y": rect_y,
        "rect_width": rect_w,
        "rect_height": rect_h,
        "spriteName": sprite_name 
    }
    
    print(f" Procesado {block_name}: Rectángulo en ({rect_x}, {rect_y}) {rect_w}x{rect_h}")

# Recorrer todas las imágenes en la carpeta
for filename in os.listdir(IMAGES_FOLDER):
    if filename.endswith((".png", ".jpg", ".jpeg")):  # Solo archivos de imagen
        analyze_block(os.path.join(IMAGES_FOLDER, filename))

# Agregamos la clave raíz "blocks"
json_data = {"blocks": block_data}

# Guardar la información en un archivo JSON
with open(OUTPUT_JSON, "w") as json_file:
    json.dump(json_data, json_file, indent=4)

print(f" Análisis completado. Datos guardados en {OUTPUT_JSON}")
