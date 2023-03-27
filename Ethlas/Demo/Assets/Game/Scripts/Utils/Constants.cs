using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public const string PLAYER_READY = "IsPlayerReady";
    public const string PLAYER_LOADED_LEVEL = "PlayerLoadedLevel";

    public static Color GetColor(int colorChoice)
    {
        switch (colorChoice)
        {
            case 0: return Color.red;
            case 1: return Color.blue;
        }

        return Color.black;
    }
}
