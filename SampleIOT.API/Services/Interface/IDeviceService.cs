using SampleIOT.API.Models;
using System.Collections;
using System.Collections.Generic;

namespace SampleIOT.API.Services.Interface
{
    public interface IDeviceService
    {
        IEnumerable<Device> GetDevices();
    }
}
