using IceTea.Pure.BaseModels;
using IceTea.Pure.Utils;
using Prism.Regions;

namespace ChinChessClient.ViewModels.Contracts
{
    internal abstract class NavigateViewModelBase : NotifyBase, IConfirmNavigationRequest, IRegionMemberLifetime
    {
        public abstract string Title { get; }

        protected bool _needWarn;

        #region IConfirmNavigationRequest
        public abstract void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback);
        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public abstract void OnNavigatedFrom(NavigationContext navigationContext);
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            var parameters = navigationContext.Parameters;

            parameters.Add(nameof(Title), this.Title);

            AppUtils.AssertOperationValidation(parameters.ContainsKey("NeedWarn"), "必须要传递NeedWarn");

            this._needWarn = (bool)parameters["NeedWarn"];
        }
        #endregion

        #region IRegionMemberLifetime
        public bool KeepAlive => false;
        #endregion
    }
}
