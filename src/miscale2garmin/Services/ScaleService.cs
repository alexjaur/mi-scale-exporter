﻿using miscale2garmin.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace miscale2garmin.Services
{
    public class ScaleService : IScaleService
    {
        private IAdapter _adapter;
        private IMetricsService _metricsService;
        private IDevice _scaleDevice;
        private Scale _scale;
        private User _user;
        private TaskCompletionSource<BodyComposition> _completionSource;
        private BodyComposition bodyComposition;

        public ScaleService(IMetricsService metricsService)
        {
            _metricsService = metricsService;
            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.ScanTimeout = 50000;
            _adapter.ScanTimeoutElapsed += TimeOuted;
        }

        public async Task<BodyComposition> GetBodyCompositonAsync(Scale scale, User user)
        {
            _completionSource = new TaskCompletionSource<BodyComposition>();
            _scale = scale;
            _user = user;
            _adapter.DeviceDiscovered += DevideDiscovered;
            await _adapter.StartScanningForDevicesAsync();
            return await _completionSource.Task;
        }

        public async Task CancelSearchAsync()
        {
            try
            {
                bodyComposition.IsValid = false;
                _completionSource.SetResult(bodyComposition);
            }
            catch (Exception ex)
            {
                // TODO Log;
            }
            await StopAsync();
        }

        private void TimeOuted(object s, EventArgs e)
        {
            StopAsync().Wait();
            bodyComposition.IsValid = false;
            _completionSource.SetResult(bodyComposition);
        }

        private void DevideDiscovered(object s, DeviceEventArgs a) {

            var obj = a.Device.NativeDevice;
            PropertyInfo propInfo = obj.GetType().GetProperty("Address");
            string address = (string)propInfo.GetValue(obj, null);

            if(address.ToLowerInvariant() == _scale.Address?.ToLowerInvariant())
            {
                try
                {
                    _scaleDevice = a.Device;
                    GetScanData();
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    StopAsync().Wait();
                    bodyComposition.IsValid = true;
                    _completionSource.SetResult(bodyComposition);
                }
            }
        }

        private void GetScanData()
        {
            if(_scaleDevice != null)
            {
                var data = _scaleDevice.AdvertisementRecords.Where(x => x.Type == Plugin.BLE.Abstractions.AdvertisementRecordType.ServiceData) //0x16
                     .Select(x => x.Data)
                     .FirstOrDefault();
                ComputeData(data);
            }
        }

        private void ComputeData(byte[] data)
        {
            var le = BitConverter.IsLittleEndian;
            var buffer = data.Skip(2).ToArray(); // checks why the array is shifted by 2 bytes
            var ctrlByte1 = buffer[1];
            var stabilized = ctrlByte1 & (1 << 5);
        
            var hasImpedance = ctrlByte1 & (1 << 1);
            var weight = (((buffer[12] & 0xFF) << 8) | (buffer[11] & 0xFF)) * 0.005;
            var impedance = (buffer[10] << 8) + buffer[9];

            if(stabilized > 0)
            {
                bodyComposition = this._metricsService.GetBodyComposition(_user, weight, impedance);
            }
        }

        private async Task StopAsync()
        {
            await _adapter.StopScanningForDevicesAsync();

            _adapter.DeviceDiscovered -= DevideDiscovered;
        }



    }
}
