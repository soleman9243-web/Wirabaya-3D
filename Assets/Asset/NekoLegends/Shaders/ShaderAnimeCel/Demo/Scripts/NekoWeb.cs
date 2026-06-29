
namespace NekoLegends
{
    public class NekoWeb : Neko
    {
        protected override void Start()
        {
            base.Start();
            _featureCount = 7;
        }

        public override int ShowNextFeature()
        {
            _featureIndex = (_featureIndex + 1) % _featureCount;
            switch (_featureIndex)
            {
                case 0:
                    NekoDemo.Instance.SetBackgroundActive(true);
                    _directionalLight.gameObject.SetActive(true);
                    _hairMaterials[0] = hairMaterials[0];

                    skinnedMeshRendererHair.materials = _hairMaterials;
                    skinnedMeshRendererHair.materials[1] = outlines[0];
                    NekoDemo.Instance.DescriptionText.SetText("Basic Cel Shading With Directional Lighting");
                    break;
                case 1:
                    _directionalLight.gameObject.SetActive(false);
                    NekoDemo.Instance.SetBackgroundActive(false);
                    NekoDemo.Instance.DescriptionText.SetText("Lights Off");
                    break;
                case 2:
                    _eyeMaterials[0] = eyeMaterials[1];
                    _eyeMaterials[1] = eyeMaterials[1];
                    skinnedMeshEyes.materials = _eyeMaterials;
                    NekoDemo.Instance.DescriptionText.SetText("Eyes Emissions Material");
                    break;
                case 3:
                    NekoDemo.Instance.SetPointLightActive(true);
                    NekoDemo.Instance.DescriptionText.SetText("Point Lighting Support");
                    break;
                case 4:
                    _hairMaterials[0] = hairMaterials[1];
                    skinnedMeshRendererHair.materials = _hairMaterials;
                    NekoDemo.Instance.DescriptionText.SetText("Point Lighting + Ambient Self Lighting");
                    break;
                case 5:
                    NekoDemo.Instance.SetPointLightActive(false);
                    NekoDemo.Instance.DescriptionText.SetText("Ambient Self Lighting Only");
                    break;
                case 6:
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


    }
}
