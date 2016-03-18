using System.Windows.Controls;
using ARFinity.XnaObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ARFinity
{
    public partial class Placemark : UserControl
    {
        internal UIElementRenderer Renderer { get; private set; }

        internal BasicShape Object3D = new BasicShape(new Vector3(4, 2, 1), new Vector3(0, 0, 0));        

        public Placemark()
        {
            InitializeComponent();

            Renderer = new UIElementRenderer(this, (int)Width, (int)Height);
        }

    }
}
