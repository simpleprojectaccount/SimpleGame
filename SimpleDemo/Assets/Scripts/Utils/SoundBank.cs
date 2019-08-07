using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vertigo.Utilities
{
    /// <summary>
    /// Simple soundbank for holding sound effects
    /// </summary>
    public class SoundBank : ScriptableObject
    {
        public List<Sound> soundEffects;

        [Serializable]
        public struct Sound
        {
            public int priority;
            public float playDelay;
            public AudioClip audio;

            public static Sound EmptySound()
            {
                return new Sound { priority = -1 };
            }
        }
    }
}