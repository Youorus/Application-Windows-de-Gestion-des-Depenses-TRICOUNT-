﻿using prbd_2324_a03.Model;
using PRBD_Framework;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace prbd_2324_a03.ViewModel
{
    public class AddTricountViewModel : ViewModelCommon {
        private readonly int _userId = CurrentUser.UserId;

        private User _selectedParticipant;
        public User SelectedParticipant {
            get => _selectedParticipant;
            set => SetProperty(ref _selectedParticipant, value);
        }

        private  TricountDetailsViewModel tricountDetailsViewModel;

        public ObservableCollectionFast<ParticipantsListViewModel> ParticipantsUsers { get; set; } = new ObservableCollectionFast<ParticipantsListViewModel>();

        private bool _isVisibleOperationTricount;
        public bool IsVisibleOperationTricount {
            get => _isVisibleOperationTricount;
            set => SetProperty(ref _isVisibleOperationTricount, value);
        }

        public ICommand AddUserCommand { get; set; }
        public ICommand AddAllUserCommand { get; set; }
        public ICommand SaveTricountCommand { get; set; }
        public ICommand CancelTricountCommand { get; set; }
        public ICommand RemoveParticipant { get; set; }
        public ICommand AddMySelfCommand { get; set; }

        public bool IsVisibleEditTricount { get; set; } = false;

        private ObservableCollectionFast<User> _allUsers;
        public ObservableCollectionFast<User> AllUsers {
            get => _allUsers;
            set => SetProperty(ref _allUsers, value);
        }

        private ObservableCollectionFast<User> _otherUsers;
        public ObservableCollectionFast<User> Users {
            get => _otherUsers;
            set => SetProperty(ref _otherUsers, value);
        }

        private bool _isCreator;
        public bool IsCreator {
            get => _isCreator;
            set => SetProperty(ref _isCreator, value);
        }
      
        private ObservableCollectionFast<User> _usersParticipants;
        public ObservableCollectionFast<User> Participants {
            get => _usersParticipants;
            set => SetProperty(ref _usersParticipants, value);
        }

        private Tricounts _tricount;
        public Tricounts Tricount {
            get => _tricount;
            set => SetProperty(ref _tricount, value);
        }

        public bool IsSavedAndValid => !IsNew && !HasChanges;
        public override bool MayLeave => IsSavedAndValid;

        private bool _isNew;
        public bool IsNew {
            get => _isNew;
            set => SetProperty(ref _isNew, value);
        }

        private string _title;
        public string Title {
            get => Tricount?.Title;
            set => SetProperty(Tricount.Title, value, Tricount, (t, v) => {
                t.Title = v;
                Validate();
                NotifyColleagues(App.Messages.MSG_TITLE_CHANGED, Tricount);
            });
        }

        private List<User> _removedParticipants = new List<User>();


        private string _description;
        public string Description {
            get => Tricount?.Description;
            set => SetProperty(Tricount.Description, value, Tricount, (t, v) => {
                t.Description = v;
                Validate();
            });
        }

        private DateTime _creationDate;
        public DateTime CreationDate {
            get => Tricount.Created_at;
            set => SetProperty(Tricount.Created_at, value, Tricount, (t, v) => {
                t.Created_at = v;
                Validate();
            });
        }

   




        public void RestoreRemovedParticipants() {
            foreach (var participant in _removedParticipants) {
                var vm = new ParticipantsListViewModel(participant, Tricount, IsNew);
                ParticipantsUsers.Add(vm);
                _otherUsers.Remove(participant);
            }
            _removedParticipants.Clear(); // Effacer la liste temporaire après la restauration
        }


        public override bool Validate() {
            ClearErrors();

            if (Context.Tricounts.Any(t => t.Title == Title && t.Creator == _userId)) {
                AddError(nameof(Title), "You already have a Tricount with this title.");
            }
           
            if (IsNew && CreationDate > DateTime.Now) {
                AddError(nameof(CreationDate), "The creation date can't be in the future");
            } 
            if (!IsNew && CreationDate < Tricount.Created_at) {
                AddError(nameof(CreationDate), "The creation date can't be before the original creation date");
            }
            if (IsNew && string.IsNullOrEmpty(Title)) {
                AddError(nameof(Title), "Title is required");
            }
            return !HasErrors;
        }

        public void UsersList() {
            var usersList = Context.Users.Where(user => user.UserId != _userId).OrderBy(user => user.Full_name).ToList();
            Users = new ObservableCollectionFast<User>(usersList ?? new List<User>());
        }

        public void AllUsersList() {
            var allUsersList = Context.Users.ToList();
            AllUsers = new ObservableCollectionFast<User>(allUsersList ?? new List<User>());
        }
     

        private void AddAction() {
            var selectItem = new ParticipantsListViewModel(SelectedParticipant, Tricount, IsNew);
            if (SelectedParticipant != null && !ParticipantsUsers.Contains(selectItem)) {
                ParticipantsUsers.Add(selectItem);
                Users.Remove(SelectedParticipant);
                SortParticipants();
            }
        }

        public override void SaveAction() {

            if (IsNew) {
                var tricount = new Tricounts {
                    Title = Title,
                    Description = string.IsNullOrEmpty(Description) ? "Description Vide" : Description,
                    Created_at = CreationDate,
                    Creator = _userId
                };

                Context.Tricounts.Add(tricount);
                Context.SaveChanges();

                foreach (var participantVm in ParticipantsUsers) {
                    var subscription = new Subscriptions {
                        TricountId = tricount.Id,
                        UserId = participantVm.User.UserId
                    };
                    Context.Subscriptions.Add(subscription);
                }

                Context.SaveChanges();
                IsNew = false;
                NotifyColleagues(App.Messages.MSG_TRICOUNT_CHANGED, Tricount);
                NotifyColleagues(App.Messages.MSG_CANCEL_TRICOUNT, Tricount);
            } else {
                // Mettre à jour les propriétés de l'objet Tricount existant
                Tricount.Title = Title;
                Tricount.Description = Description;
                Tricount.Created_at = CreationDate;

                // Gérer les participants (ajouts et suppressions)
                var existingParticipants = Context.Subscriptions.Where(s => s.TricountId == Tricount.Id).ToList();

                // Supprimer les participants qui ne sont plus dans la liste
                foreach (var participant in existingParticipants) {
                    if (!ParticipantsUsers.Any(p => p.User.UserId == participant.UserId)) {
                        Context.Subscriptions.Remove(participant);
                    }
                }

                // Ajouter les nouveaux participants
                foreach (var participantVm in ParticipantsUsers) {
                    if (!existingParticipants.Any(ep => ep.UserId == participantVm.User.UserId)) {
                        var newSubscription = new Subscriptions {
                            TricountId = Tricount.Id,
                            UserId = participantVm.User.UserId
                        };
                        Context.Subscriptions.Add(newSubscription);
                    }
                }

                Context.SaveChanges();
                // Rétablir la visibilité appropriée
                tricountDetailsViewModel.IsVisibleDetailsTricount = false;
                tricountDetailsViewModel.IsVisibleOperationTricount = true;


                NotifyColleagues(App.Messages.MSG_TRICOUNT_CHANGED, Tricount);
                NotifyColleagues(App.Messages.MSG_VIEWTRICOUNT_CHANGED, Tricount);


            }

        }



        private void SortParticipants() {
            var sortedParticipants = ParticipantsUsers.OrderBy(p => p.User.Full_name).ToList();
            ParticipantsUsers.Clear();
            foreach (var participant in sortedParticipants) {
                ParticipantsUsers.Add(participant);
            }
        }

        private void CancelTricount() {
            if (IsNew) {
                IsNew = false;
                NotifyColleagues(App.Messages.MSG_CANCEL_TRICOUNT, Tricount);
            } else {
                Tricount.Reload();
                RaisePropertyChanged();
            }
        }

        private void AddAllUsersAction() {
            foreach (var user in Users) {
                var x = new ParticipantsListViewModel(user, Tricount, IsNew);
                if (!ParticipantsUsers.Contains(x)) {
                    ParticipantsUsers.Add(x);
                    SortParticipants(); 
                }
            }
            Users.Clear();
        }

        private void AddMySelfAction() {
            var user = Context.Users.FirstOrDefault(u => u.UserId == _userId);
            if (user != null && !ParticipantsUsers.Any(p => p.User.UserId == user.UserId)) {
                var participantVm = new ParticipantsListViewModel(user, Tricount,IsNew);
                ParticipantsUsers.Add(participantVm);
                Users.Remove(user);
            }
        }

        private bool CanCancelAction() {
            return Tricount != null && (IsNew || Tricount.IsModified);
        }

        public void RemoveParticipantAction(ParticipantsListViewModel participantToRemove) {
            if (participantToRemove != null) {
                ParticipantsUsers.Remove(participantToRemove);
                _otherUsers.Add(participantToRemove.User);
                SortParticipants();
            }
        }





        public AddTricountViewModel(Tricounts tricount, bool isNew, TricountDetailsViewModel tricountDetailsViewModel) {
            Tricount = tricount;
            IsNew = isNew;
            this.tricountDetailsViewModel = tricountDetailsViewModel;

            RaisePropertyChanged();
            OnRefreshData();

            AddUserCommand = new RelayCommand(AddAction, () => SelectedParticipant != null);
            AddAllUserCommand = new RelayCommand(AddAllUsersAction, () => Users.Any());
              SaveTricountCommand = new RelayCommand(SaveAction, CanSaveAction);

            AddMySelfCommand = new RelayCommand(AddMySelfAction, () => !ParticipantsUsers.Any(p => p.User.UserId == _userId));
            RemoveParticipant = new RelayCommand<ParticipantsListViewModel>(RemoveParticipantAction);
            CancelTricountCommand = new RelayCommand(CancelTricount, CanCancelAction);
        }



        private bool CanSaveAction() {
            return !string.IsNullOrEmpty(Title) && !HasErrors;
        }

        protected override void OnRefreshData() {
            UsersList();

            _creationDate = DateTime.Now;

            if (IsNew) {
                var user = Context.Users.FirstOrDefault(u => u.UserId == _userId);
                var vm = new ParticipantsListViewModel(user, Tricount,IsNew);
                ParticipantsUsers.Add(vm);
            } else {
                var participants = Context.Subscriptions
                    .Where(s => s.TricountId == Tricount.Id)
                    .Select(s => s.User).OrderBy(u => u.Full_name)
                    .ToList();

                foreach (var item in participants) {
                    var vm = new ParticipantsListViewModel(item, Tricount, IsNew);
                    ParticipantsUsers.Add(vm);
                    _otherUsers.Remove(item);   
                }
            }

           
        }
    }
}
