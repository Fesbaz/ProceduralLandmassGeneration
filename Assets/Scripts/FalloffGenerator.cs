using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// falloff map so that, should we wish, we can force landmasses to be surrounded by water.
public static class FalloffGenerator {
    
    public static float[,] GenerateFalloffMap(int size) {
        float[,] map = new float[size,size];

        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i,j] = Evaluate(value);
            }
        }

        return map;
    }

    static float Evaluate(float value) {
        float a = 3;    // straightness of curve, try 1-10
        float b = 2.2f; // shift function right/left with higher/lower values. Left => 0, right => 1. Try 1-10

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a)); // formula for messing with falloff map. Pow are slow, but falloff map generated only once at start
    }
}
