using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace RotateController
{
    public class RotateController : MonoBehaviour
    {
        //public Text fpsText;
        public Space m_RotateSpace;
        public float m_RotateSpeed = 30f;
		public bool m_Revers = false;
        private int frameCount = 0;
        private float passedTime = 0;
        private float fps = 0;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButton(0))
            {
				int revers = 1;
				if(m_Revers)
				{
					revers = -1;
				}
				
				float rSpeed = 60*revers;
                if (Input.mousePosition.x <= Screen.width * 0.5)
                {
                    rSpeed = -60*revers;
                }
                transform.Rotate(Vector3.up * rSpeed * Time.deltaTime, m_RotateSpace);
            }
            else
            {
                transform.Rotate(Vector3.up * m_RotateSpeed * Time.deltaTime, m_RotateSpace);
            }

            //fpsText.text = "FPS:"+getFPS();
            //Debug.Log(getFPS());
        }

        /*void OnMouseDown()
        {
            //this.transform.Rotate(Vector3.up * m_RotateSpeed * Time.deltaTime, m_RotateSpace);
            Debug.Log("hahaha");
        }*/

        private void initializeFPS()
        {

        }

        private float getFPS()
        {
            frameCount++;
            passedTime = passedTime + Time.deltaTime;
            if (passedTime > 2)
            {
                fps = frameCount / passedTime;
                frameCount = 0;
                passedTime = 0;
            }
            return fps;
        }
    }
}
