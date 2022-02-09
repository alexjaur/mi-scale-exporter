﻿using miscale2garmin.Models;
using miscale2garmin.Services;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;

namespace miscale2garmin.ViewModels
{
    public class EntryViewModel : BaseViewModel, IEntryViewModel
    {
        private IScaleService ScaleService;
        public EntryViewModel(IScaleService scaleService)
        {
            ScaleService = scaleService;
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            this.PropertyChanged +=
                (_, __) => SaveCommand.ChangeCanExecute();


            Title = "Form";
            OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://github.com/lswiderski/miscale2garmin-mobile"));
            ScanCommand = new Command(async () =>
            {

                Scale scale = new Scale()
                {
                    Address = Address,
                };
                ScanningLabel = "Scanning";
               var bc = await ScaleService.GetBodyCompositonAsync(scale, new User { Sex = sex, Age = Age, Height = Height});

                if (bc.IsValid)
                {
                    ScanningLabel = "";
                }
                else
                {
                    ScanningLabel = "Not found";
                }

                Weight = bc.Weight;
                OnPropertyChanged(nameof(Weight));

            });
        }

        private bool ValidateSave()
        {
            return !String.IsNullOrWhiteSpace(address);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        private async void OnCancel()
        {
            await ScaleService.CancelSearchAsync();
        }

        private async void OnSave()
        {

            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("..");
        }

        public void SexRadioButtonChanged(object s, CheckedChangedEventArgs e)
        {
            var radio = s as RadioButton;
            this.Sex = radio.Value as string == "1" ? Models.Sex.Male : Models.Sex.Female;
        }


        public ICommand OpenWebCommand { get; }

        public ICommand ScanCommand { get; }

        private string address;
        public string Address
        {
            get => address;
            set => SetProperty(ref address, value);
        }

        private int age;
        public int Age
        {
            get => age;
            set => SetProperty(ref age, value);
        }

        private int height;
        public int Height
        {
            get => height;
            set => SetProperty(ref height, value);
        }

        private Sex sex;
        public Sex Sex
        {
            get => sex;
            set => SetProperty(ref sex , value);
        }

        private string scanningLabel;
        public string ScanningLabel
        {
            get => scanningLabel;
            set => SetProperty(ref scanningLabel, value);
        }

        private double weight;
        public double Weight
        {
            get => weight;
            set => SetProperty(ref weight, value);
        }


    }
}