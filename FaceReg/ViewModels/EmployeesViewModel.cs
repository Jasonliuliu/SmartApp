using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Acr.UserDialogs;

using Microsoft.ProjectOxford.Face;

namespace FaceReg.ViewModels
{
    public class EmployeesViewModel : BaseViewModel
    {
        string personGroupId;

        public EmployeesViewModel()
        {
            Title = "Employees";

            Employees = new ObservableCollection<Employee>
            {
                new Employee { Name = "yong chen", Title = "CEO", PhotoUrl = "http://bosxixi.me/1.jpg" },
                new Employee { Name = "Miguel de Icaza", Title = "CTO", PhotoUrl = "http://images.techhive.com/images/idge/imported/article/nww/2011/03/031111-deicaza-100272676-orig.jpg" },
                new Employee { Name = "Joseph Hill", Title = "VP of Developer Relations", PhotoUrl = "https://www.gravatar.com/avatar/f763ec6935726b7f7715808828e52223.jpg?s=256" },
                new Employee { Name = "James Montemagno", Title = "Developer Evangelist", PhotoUrl = "http://www.gravatar.com/avatar/7d1f32b86a6076963e7beab73dddf7ca?s=256" },
                new Employee { Name = "Pierce Boggan", Title = "Software Engineer", PhotoUrl = "https://avatars3.githubusercontent.com/u/1091304?v=3&s=460" },
            };

            RegisterEmployees();
        }

        ObservableCollection<Employee> employees;
        public ObservableCollection<Employee> Employees
        {
            get { return employees; }
            set { employees = value; OnPropertyChanged("Employees"); }
        }
        string newName;
        public string NewName
        {
            get { return newName; }
            set { newName = value; OnPropertyChanged("NewName"); }
        }
        string newUrl;
        public string NewUrl
        {
            get { return newUrl; }
            set { newUrl = value; OnPropertyChanged("NewUrl"); }
        }
        bool newStackVisible;
        public bool NewStackVisible
        {
            get { return newStackVisible; }
            set { newStackVisible = value; OnPropertyChanged("NewStackVisible"); }
        }

        Command findSimilarFaceCommand;
        public Command FindSimilarFaceCommand
        {
            get { return findSimilarFaceCommand ?? (findSimilarFaceCommand = new Command(async () => await ExecuteFindSimilarFaceCommandAsync())); }
        }
        Command addCommand;
        public Command AddCommand
        {
            get { return addCommand ?? (addCommand = new Command(async () => await ExcuteAddCommand())); }
        }
        Command newCommand;
        public Command NewCommand
        {
            get { return newCommand ?? (newCommand = new Command(() => ExcuteNewCommand())); }
        }
        void ExcuteNewCommand()
        {
            this.NewStackVisible = !this.NewStackVisible;
        }
        async Task ExcuteAddCommand()
        {
            if (String.IsNullOrEmpty(this.NewName) && String.IsNullOrEmpty(this.NewUrl))
            {
                await UserDialogs.Instance.AlertAsync($"Employee Name and Photo Url must provide.");
                return;
            }
            try
            {
                var http = new System.Net.Http.HttpClient();
                var pho = await http.GetStreamAsync(this.NewUrl);
            }
            catch (Exception)
            {
                await UserDialogs.Instance.AlertAsync($"Photo Url is not a valid resource.");
                return;
            }
            var emp = new Employee(){Name = this.NewName, Title="Employee", PhotoUrl=this.NewUrl};
            try
            {
                await RegisterEmployee(emp);
            }
            catch (Exception)
            {
                await UserDialogs.Instance.AlertAsync($"RegisterEmployee fail.");

                return;
            }
           
          await  UserDialogs.Instance.AlertAsync($"employee added.");
            this.NewStackVisible = false;
            this.NewName = string.Empty;
            this.NewUrl = string.Empty;
        }
        async Task ExecuteFindSimilarFaceCommandAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                MediaFile photo;

                await CrossMedia.Current.Initialize();

                // Take photo
                if (CrossMedia.Current.IsCameraAvailable)
                {
                    photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                    {
                        Directory = "Employee Directory",
                        Name = "photo.jpg"
                    });
                }
                else
                {
                    photo = await CrossMedia.Current.PickPhotoAsync();
                }
                //var http = new System.Net.Http.HttpClient();
                //var pho = await http.GetStreamAsync("http://bosxixi.me/1.jpg");
                // Upload to cognitive services
                using (var stream = pho)
                {
                    var faceServiceClient = new FaceServiceClient("f989b4fe48244394a570dfa1ac336f9e");

                    // Step 4 - Upload our photo and see who it is!
                    var faces = await faceServiceClient.DetectAsync(stream);
                    var faceIds = faces.Select(face => face.FaceId).ToArray();

                    var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                    if (!results.Any())
                    {
                        UserDialogs.Instance.Alert($"Person not identified.");
                    }
                    var result = results[0].Candidates[0].PersonId;

                    var person = await faceServiceClient.GetPersonAsync(personGroupId, result);

                    UserDialogs.Instance.Alert($"Person identified is {person.Name}.");
                }
            }
            catch (Exception ex)
            {
                UserDialogs.Instance.Alert(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        async Task RegisterEmployees()
        {
            this.IsBusy = true;
            var faceServiceClient = new FaceServiceClient("f989b4fe48244394a570dfa1ac336f9e");

            // Step 1 - Create Face List
            personGroupId = Guid.NewGuid().ToString();
            try
            {
                await faceServiceClient.CreatePersonGroupAsync(personGroupId, "Xamarin Employees");
  
            }
            catch (Exception ex)
            {
                if (ex is Microsoft.ProjectOxford.Face.FaceAPIException e)
                {
                    UserDialogs.Instance.Alert(e.ErrorMessage);
                    return;
                }
            }
           
            // Step 2 - Add people to face list
            foreach (var employee in Employees)
            {
                var p = await faceServiceClient.CreatePersonAsync(personGroupId, employee.Name);
                await faceServiceClient.AddPersonFaceAsync(personGroupId, p.PersonId, employee.PhotoUrl);
            }
            // Step 3 - Train face group
            await faceServiceClient.TrainPersonGroupAsync(personGroupId);

            this.IsBusy = false;
        }
        async Task RegisterEmployee(Employee emp)
        {
            var faceServiceClient = new FaceServiceClient("f989b4fe48244394a570dfa1ac336f9e");

            this.Employees.Insert(0, emp);


            var p = await faceServiceClient.CreatePersonAsync(personGroupId, emp.Name);
            await faceServiceClient.AddPersonFaceAsync(personGroupId, p.PersonId, emp.PhotoUrl);
      
            await faceServiceClient.TrainPersonGroupAsync(personGroupId);
        }
    }
}
