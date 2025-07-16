using SampleIOT.API.Models;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleIOT.API.Services.Interface
{
    public interface IDeviceService
    {
        IEnumerable<Device> GetDevices();
        Device GetDevice(string id);

        Task Start();
    }
}
