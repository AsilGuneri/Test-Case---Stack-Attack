using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace StackAttack.Menu
{
    public sealed class MenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private MenuView view;

        protected override void Configure(IContainerBuilder builder)
        {
            if (view == null)
            {
                throw new MissingReferenceException("MenuLifetimeScope requires a MenuView.");
            }

            builder.RegisterComponent(view);
            builder.RegisterEntryPoint<MenuPresenter>();
        }
    }
}
