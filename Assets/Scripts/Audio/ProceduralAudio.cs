using System;
using UnityEngine;

namespace LudoGame.Audio
{
    // Every clip here is generated from real waveform math (sine tones, filtered noise,
    // envelopes) at runtime - no external audio assets, and nothing is silent/placeholder.
    public static class ProceduralAudio
    {
        private const int SampleRate = 44100;

        public static AudioClip DiceRoll()
        {
            float duration = 0.6f;
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];
            var rng = new System.Random(1);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;
                // Rattling percussive noise bursts, decaying in frequency and amplitude -
                // approximates dice clattering inside a cup/hand.
                float noise = ((float)rng.NextDouble() * 2f - 1f);
                float envelope = Mathf.Pow(1f - progress, 1.5f);
                float clickRate = Mathf.Lerp(28f, 6f, progress); // rattles slow down as it "settles"
                float gate = (Mathf.Sin(t * clickRate * Mathf.PI * 2f) > 0.3f) ? 1f : 0.15f;
                data[i] = noise * envelope * gate * 0.5f;
            }
            return BuildClip("DiceRoll", data);
        }

        public static AudioClip TokenMove()
        {
            float duration = 0.12f;
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(520f, 380f, progress); // short downward "hop" blip
                float envelope = Mathf.Sin(progress * Mathf.PI); // fades in and out smoothly
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f;
            }
            return BuildClip("TokenMove", data);
        }

        public static AudioClip Capture()
        {
            float duration = 0.35f;
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];
            var rng = new System.Random(2);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;
                // A sharp descending tone plus a short noise "pop" for impact.
                float tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 150f, progress) * t);
                float pop = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-progress * 18f);
                float envelope = Mathf.Exp(-progress * 4f);
                data[i] = (tone * 0.5f + pop * 0.6f) * envelope;
            }
            return BuildClip("Capture", data);
        }

        public static AudioClip ButtonClick()
        {
            float duration = 0.07f;
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-t * 60f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 1200f * t) * envelope * 0.3f;
            }
            return BuildClip("ButtonClick", data);
        }

        public static AudioClip TurnNotification()
        {
            float duration = 0.3f;
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];
            // Two-note gentle chime (major third) so it reads as "your turn", not an alarm.
            float f1 = 660f, f2 = 880f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;
                float note = progress < 0.4f ? f1 : f2;
                float localEnv = Mathf.Exp(-((progress < 0.4f ? progress : progress - 0.4f)) * 8f);
                data[i] = Mathf.Sin(2f * Mathf.PI * note * t) * localEnv * 0.4f;
            }
            return BuildClip("TurnNotification", data);
        }

        public static AudioClip Victory()
        {
            float duration = 1.4f;
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];
            // Simple ascending major arpeggio (C-E-G-C) - a real musical fanfare, generated,
            // not a copied melody.
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            float noteDuration = duration / notes.Length;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIndex = Mathf.Min((int)(t / noteDuration), notes.Length - 1);
                float noteT = t - noteIndex * noteDuration;
                float envelope = Mathf.Exp(-noteT * 3f) * Mathf.Sin(Mathf.Clamp01(noteT / 0.02f) * Mathf.PI * 0.5f);
                float freq = notes[noteIndex];
                // Add a light harmonic for a fuller "bell" timbre.
                data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f
                         + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.2f) * envelope * 0.5f;
            }
            return BuildClip("Victory", data);
        }

        public static AudioClip PlayerJoin()
        {
            float duration = 0.25f;
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(440f, 660f, progress); // rising blip = "joined"
                float envelope = Mathf.Sin(progress * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f;
            }
            return BuildClip("PlayerJoin", data);
        }

        public static AudioClip PlayerLeave()
        {
            float duration = 0.25f;
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(660f, 380f, progress); // falling blip = "left"
                float envelope = Mathf.Sin(progress * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f;
            }
            return BuildClip("PlayerLeave", data);
        }

        public static AudioClip GameStart()
        {
            float duration = 0.5f;
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];
            float[] notes = { 392f, 523.25f }; // G4 -> C5, short confident "go" cue
            float noteDuration = duration / notes.Length;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIndex = Mathf.Min((int)(t / noteDuration), notes.Length - 1);
                float noteT = t - noteIndex * noteDuration;
                float envelope = Mathf.Exp(-noteT * 4f);
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) * envelope * 0.45f;
            }
            return BuildClip("GameStart", data);
        }

        private static AudioClip BuildClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
