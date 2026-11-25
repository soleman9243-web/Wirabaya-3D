// Drop this on any GameObject, assign the variations of one family, and call
// Play() from a button. It never plays the same clip twice in a row, and it
// never repeats until the whole set has been used.
//
// That last part is the point of this pack. Plain Random.Range repeats: with
// 12 clips it plays the same one twice in a row about 8% of the time, and that
// is exactly the repetition you paid to avoid. A shuffled bag cannot.
//
// The file name has to stay RandomSfx.cs. Unity refuses to attach a
// MonoBehaviour whose file name does not match its class name.

using UnityEngine;

namespace Willosuna.EssentialUI
{
    [RequireComponent(typeof(AudioSource))]
    public class RandomSfx : MonoBehaviour
    {
        public AudioClip[] variations;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Small pitch spread on top of the built-in variation. Keep it low; " +
                 "these clips already differ from each other.")]
        [Range(0f, 0.2f)] public float pitchJitter = 0.03f;

        AudioSource _source;
        int[] _bag;
        int _next;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            Reshuffle();
        }

        void Reshuffle()
        {
            if (variations == null || variations.Length == 0) return;
            int last = (_bag != null && _bag.Length > 0) ? _bag[_bag.Length - 1] : -1;
            if (_bag == null || _bag.Length != variations.Length)
            {
                _bag = new int[variations.Length];
                for (int i = 0; i < _bag.Length; i++) _bag[i] = i;
            }
            // Fisher-Yates, then keep the first pick from matching the last one
            // played so a reshuffle cannot produce a back-to-back repeat.
            for (int i = _bag.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
            if (_bag.Length > 1 && _bag[0] == last) (_bag[0], _bag[1]) = (_bag[1], _bag[0]);
            _next = 0;
        }

        public void Play()
        {
            if (variations == null || variations.Length == 0) return;
            // _bag is null when variations was still empty at Awake, and stale when
            // clips are assigned at runtime. Either way it has to be rebuilt before
            // it is indexed.
            if (_bag == null || _bag.Length != variations.Length || _next >= _bag.Length)
                Reshuffle();
            if (_source == null) _source = GetComponent<AudioSource>();
            _source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            _source.PlayOneShot(variations[_bag[_next++]], volume);
        }
    }
}
