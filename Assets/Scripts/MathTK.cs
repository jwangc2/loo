using UnityEngine;
using System.Collections;

public static class MathTK {
    public static float Sign(float n)
    {
        return (n > 0) ? 1f : ((n < 0) ? -1f : 0f);
    }
}
