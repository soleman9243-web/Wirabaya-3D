
using System;
using UnityEngine;


namespace NekoLegends
{
    public class Neko : MonoBehaviour
    {
        [SerializeField] protected AnimationClip[] poses;
        [SerializeField] protected SkinnedMeshRenderer skinnedMeshRendererHair, skinnedMeshEyes;
        [SerializeField] protected Material[] hairMaterials, eyeMaterials;
        [SerializeField] protected Material[] outlines;

        public Animator animator { get; private set; }


        protected int _featureIndex, _currentPoseIndex = 0;

        protected float _transitionDuration = 0.1f;
        protected Light _directionalLight;
        protected Material[] _hairMaterials, _eyeMaterials;

        protected int _featureCount = 11;
        protected virtual void Start()
        {
            animator = GetComponent<Animator>();
            GameObject lightObj = GameObject.Find("Directional Light");

            // If a directional light was found, get its Light component
            if (lightObj != null)
            {
                _directionalLight = lightObj.GetComponent<Light>();
            }


            _hairMaterials = skinnedMeshRendererHair.materials;
            _eyeMaterials = skinnedMeshEyes.materials;
            NekoDemo.Instance.DescriptionText.SetText("Basic Cel Shading With Directional Lighting");
        }

        public virtual int ShowNextFeature()
        {
            _featureIndex = (_featureIndex + 1) % _featureCount;
            switch (_featureIndex)
            {
                case 0:
                    NekoDemo.Instance.SetBackgroundActive(true);
                    _directionalLight.gameObject.SetActive(true);
                    _hairMaterials[0] = hairMaterials[0];
                    skinnedMeshRendererHair.materials = _hairMaterials;
                    if (skinnedMeshRendererHair.materials.Length > 1)
                        RemoveLastMaterialFromRenderer(skinnedMeshRendererHair);
                    NekoDemo.Instance.DescriptionText.SetText("Basic Cel Shading With Directional Lighting");
                    break;
                case 1:
                    NekoDemo.Instance.DescriptionText.SetText("Black Outline Thin");
                    AddMaterialToRenderer(skinnedMeshRendererHair, outlines[0]);
                    break;
                case 2:
                    RemoveLastMaterialFromRenderer(skinnedMeshRendererHair);
                    NekoDemo.Instance.DescriptionText.SetText("Black Outline Thick");
                    AddMaterialToRenderer(skinnedMeshRendererHair, outlines[1]);
                    break;
                case 3:
                    RemoveLastMaterialFromRenderer(skinnedMeshRendererHair);
                    NekoDemo.Instance.DescriptionText.SetText("White Outline");
                    AddMaterialToRenderer(skinnedMeshRendererHair, outlines[2]);
                    break;
                case 4:
                    _directionalLight.gameObject.SetActive(false);
                    NekoDemo.Instance.SetBackgroundActive(false);
                    NekoDemo.Instance.DescriptionText.SetText("Lights Off");
                    break;
                case 5:
                    _eyeMaterials[0] = eyeMaterials[1];
                    _eyeMaterials[1] = eyeMaterials[1];
                    skinnedMeshEyes.materials = _eyeMaterials;
                    NekoDemo.Instance.DescriptionText.SetText("Eyes Emissions Material");
                    break;
                case 6:
                    NekoDemo.Instance.SetPointLightActive(true);
                    NekoDemo.Instance.DescriptionText.SetText("Point Lighting Support");
                    break;
                case 7:
                    _hairMaterials[0] = hairMaterials[1];
                    skinnedMeshRendererHair.materials = _hairMaterials;
                    NekoDemo.Instance.DescriptionText.SetText("Point Lighting + Ambient Self Lighting");
                    break;
                case 8:
                    NekoDemo.Instance.SetPointLightActive(false);
                    NekoDemo.Instance.DescriptionText.SetText("Ambient Self Lighting Only");
                    break;
                case 9:
                    _directionalLight.gameObject.SetActive(true);
                    NekoDemo.Instance.SetBackgroundActive(true);
                    NekoDemo.Instance.SetPointLightActive(false);
                    _hairMaterials[0] = hairMaterials[2];
                    skinnedMeshRendererHair.materials = _hairMaterials;
                    NekoDemo.Instance.DescriptionText.SetText("Ghost");
                    break;
            }
            return (_featureIndex);
        }

        public int GetFeatureCount()
        {
            return this._featureCount;
        }
        public void NextPose()
        {
            // Increment the pose index
            _currentPoseIndex++;

            // If the index exceeds the array length, wrap around to the beginning
            if (_currentPoseIndex >= poses.Length)
            {
                _currentPoseIndex = 0;
            }

            // Get the next animation clip
            AnimationClip nextPose = poses[_currentPoseIndex];
           
            // Play the next animation clip
            this.animator.CrossFade(nextPose.name, _transitionDuration);
            NekoDemo.Instance.DescriptionText.SetText("Pose Animation: " + nextPose.name);
        }

        public void AddMaterialToRenderer(SkinnedMeshRenderer renderer, Material materialToAdd)
        {
            Material[] currentMaterials = renderer.materials;
            Material[] newMaterials = new Material[currentMaterials.Length + 1];
            currentMaterials.CopyTo(newMaterials, 0);
            newMaterials[currentMaterials.Length] = materialToAdd;
            renderer.materials = newMaterials;
        }
        public void RemoveLastMaterialFromRenderer(SkinnedMeshRenderer renderer)
        {
            Material[] currentMaterials = renderer.materials;
            Material[] newMaterials = new Material[currentMaterials.Length - 1];
            Array.Copy(currentMaterials, newMaterials, newMaterials.Length);
            renderer.materials = newMaterials;
        }

        public void ToggleOutlines(bool isOutlines)
        {
           
                // remove outline material if it exists
                RemoveLastMaterialFromRenderer(skinnedMeshRendererHair);
           
        }

    }
}
