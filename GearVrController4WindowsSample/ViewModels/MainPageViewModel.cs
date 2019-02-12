using GearVrController4Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GearVrController4WindowsSample.ViewModels
{
    public class MainPageViewModel : GearVrController4Windows.ObservableObject
    {
        private GearVrController gearVrController;

        public GearVrController GearVrController
        {
            get => gearVrController;
            set => SetPropertyValue(ref gearVrController, value);
        }

    }
}
