using System;

namespace TarkovClient.Utils
{
    /// <summary>
    /// Clase de utilidad para calcular la matriz de transformación por mapa
    /// Soporta escalado dinámico de mapa según el tamaño de la ventana en modo PiP
    /// </summary>
    public static class MapTransformCalculator
    {
        /// <summary>
        /// Calcular matriz de transformación para mapa Factory
        /// </summary>
        /// <param name="width">Ancho de ventana</param>
        /// <param name="height">Alto de ventana</param>
        /// <returns>Cadena de matriz de transformación CSS</returns>
        public static string CalculateFactoryMapTransform(double width, double height)
        {
            // Valor base (300x250, mapa Factory)
            const double baseWidth = 300;
            const double baseHeight = 250;
            const double baseTransX = -93.2495;
            const double baseTransY = -105.55;

            // Puntos de datos medidos reales
            // Cerca de cuadrado: 300x250, 450x375, 600x500, 800x640, 1000x800, 1200x960
            // Proporción extrema: 1000x500(0.276855, -23.665, -262.629), 500x1000(0.276855, -276.194, -24.2515)

            // 1. Análisis de relación de aspecto
            double aspectRatio = width / height;
            double baseAspectRatio = baseWidth / baseHeight; // 1.2
            double aspectDifference = Math.Abs(aspectRatio - baseAspectRatio);

            // 2. Cálculo de valor de escala (basado en área)
            double sizeRatio = Math.Sqrt((width * height) / (baseWidth * baseHeight));
            double newScale;
            
            // Aplicar reducción de escala en proporciones extremas
            if (aspectDifference >= 0.5)
            {
                // Proporción extrema: Calcular escala de forma más conservadora
                newScale = 0.12 * (1 + 0.423 * (sizeRatio - 1));
            }
            else
            {
                // Cerca de cuadrado: Mantener fórmula existente
                newScale = 0.12 * (1 + 1.375 * (sizeRatio - 1));
            }

            // 3. Cálculo de proporción de eje individual
            double widthRatio = width / baseWidth;
            double heightRatio = height / baseHeight;

            // 4. Cálculo de traslación (considerando relación de aspecto)
            if (aspectDifference < 0.5) // Cerca de cuadrado (usar fórmula existente)
            {
                double newTransX = baseTransX * (1 + 1.58 * (sizeRatio - 1));
                double newTransY = baseTransY * (1 + 1.68 * (sizeRatio - 1));
                return $"matrix({newScale:F6}, 0, 0, {newScale:F6}, {newTransX:F4}, {newTransY:F4})";
            }
            else // Proporción extrema (nueva fórmula)
            {
                // En proporciones extremas, el eje largo se mueve menos, el eje corto se mueve más
                // 1000x500: Aplicar patrón X=-23.665, Y=-262.629
                
                if (width > height) // Caso horizontal largo
                {
                    // X se mueve menos (eje largo), Y se mueve más (eje corto)
                    double newTransX = baseTransX * (-0.737) * widthRatio;
                    double newTransY = baseTransY * 1.284 * heightRatio;
                    return $"matrix({newScale:F6}, 0, 0, {newScale:F6}, {newTransX:F4}, {newTransY:F4})";
                }
                else // Caso vertical largo
                {
                    // X se mueve mucho (eje corto), Y se mueve poco (eje largo)
                    // Ajuste de coeficientes basado en datos 400x600 y 500x1000
                    double newTransX = baseTransX * 1.52 * widthRatio;
                    double newTransY = baseTransY * 0.315 * heightRatio;
                    return $"matrix({newScale:F6}, 0, 0, {newScale:F6}, {newTransX:F4}, {newTransY:F4})";
                }
            }
        }

        // Se agregarán otros mapas en el futuro
        // public static string CalculateCustomsMapTransform(double width, double height) { ... }
        // public static string CalculateWoodsMapTransform(double width, double height) { ... }
        // public static string CalculateInterchangeMapTransform(double width, double height) { ... }
    }
}