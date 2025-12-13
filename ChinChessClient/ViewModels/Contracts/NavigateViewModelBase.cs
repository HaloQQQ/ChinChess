using IceTea.Pure.BaseModels;
using Prism.Regions;

namespace ChinChessClient.ViewModels.Contracts
{
    internal abstract class NavigateViewModelBase : NotifyBase, IConfirmNavigationRequest, IRegionMemberLifetime
    {
        public abstract string Title { get; }

        #region IConfirmNavigationRequest
        public abstract void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback);
        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public abstract void OnNavigatedFrom(NavigationContext navigationContext);
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            navigationContext.Parameters.Add(nameof(Title), this.Title);
        }
        #endregion

        #region IRegionMemberLifetime
        public bool KeepAlive => false;
        #endregion
    }
}
