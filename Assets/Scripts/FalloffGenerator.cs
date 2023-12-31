using UnityEngine;
using System.Collections;

public static class FalloffGenerator
{

    // ��������� ����� ��������� (falloff) ��������� �������
    public static float[,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                // ���������� ��������������� ��������� � ��������� �� -1 �� 1
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                // ���������� �������� ��� ������ ����� �� ������ ������������� �������� �� ���
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                // ������ (�������������) �������� � �������������� �������
                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    // ������ �������� �� ������ ��������� ��������� value
    static float Evaluate(float value)
    {
        float a = 3;
        float b = 2.2f;

        // ������� ��� ������ �������� � �������������� �������� ���������� a � b
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
