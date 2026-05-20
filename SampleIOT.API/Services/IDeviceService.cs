using SampleIOT.API.Models;
using System.Collections;
using System.Collections.Generic;

namespace SampleIOT.API.Services
{
    public interface IDeviceService
    {
        IEnumerable<Device> GetDevices();
        Device GetDevice(string id);
    }
}
