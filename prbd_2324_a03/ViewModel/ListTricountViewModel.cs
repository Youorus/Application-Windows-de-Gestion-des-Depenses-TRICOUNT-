﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PRBD_Framework;
using prbd_2324_a03.Model;

namespace prbd_2324_a03.ViewModel
{
    public class ListTricountViewModel : ViewModelCommon
    {
        private readonly int _userId = CurrentUser.UserId;

      

        public ICommand NewTricount { get; set; }
        public ICommand TricountDetailsView { get; set; }

        public ObservableCollection<TricountCardViewModel> Tricounts { get; set; } = new ObservableCollection<TricountCardViewModel>();

        public ListTricountViewModel() {

            NewTricount = new RelayCommand(() => { NotifyColleagues(App.Messages.MSG_NEW_TRICOUNT, new Tricounts()); });

            TricountDetailsView = new RelayCommand<TricountDetailsViewModel>(vm => { NotifyColleagues(App.Messages.MSG_DISPLAY_TRICOUNT, vm.Tricount); });

            OnRefreshData();
            Register<Tricounts>(App.Messages.MSG_TRICOUNT_CHANGED, tricount => OnRefreshData());
        }

        protected override void OnRefreshData() {
            Tricounts.Clear();
            var tricountsCreatedByUser = Context.Tricounts
         .Where(t => t.Creator == CurrentUser.UserId)
         .OrderByDescending(t => t.Created_at);

            var tricountsParticipatedByUser = Context.Tricounts
       .Where(t => t.Subscriptions.Any(s => s.UserId == CurrentUser.UserId))
       .OrderByDescending(t => t.Created_at);


            var allTricounts = tricountsCreatedByUser
        .Union(tricountsParticipatedByUser)
        .OrderByDescending(t => t.Created_at)
        .ToList();

            foreach (var tricount in allTricounts) {
                var viewModel = new TricountCardViewModel(tricount);
                Tricounts.Add(viewModel);
            }

        }

       
    }
}
