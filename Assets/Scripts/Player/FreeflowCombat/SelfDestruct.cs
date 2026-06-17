using UnityEngine;
using System.Collections;
using HitAandSlashes;

namespace HitAandSlashes
{
    public class SelfDestruct : MonoBehaviour
    {
        public float selfdestruct_in = 1.1f; // Setting this to 0 means no selfdestruct.

        void Start()
        {
            if (selfdestruct_in != 0)
            {
                Destroy(gameObject, selfdestruct_in);
            }
        }
    }

}


