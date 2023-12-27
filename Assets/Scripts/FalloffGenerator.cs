using UnityEngine;
using System.Collections;

public static class FalloffGenerator
{

    // √енераци€ карты затухани€ (falloff) заданного размера
    public static float[,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                // ¬ычисление нормализованных координат в диапазоне от -1 до 1
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                // ¬ычисление значени€ дл€ каждой точки на основе максимального значени€ по оси
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                // ќценка (экстрапол€ци€) значени€ с использованием формулы
                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    // ќценка значени€ на основе заданного параметра value
    static float Evaluate(float value)
    {
        float a = 3;
        float b = 2.2f;

        // ‘ормула дл€ оценки значени€ с использованием заданных параметров a и b
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
