using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Fluent.Validation;
using WalletWasabi.Fluent.ViewModels.CoinJoinProfiles;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Models;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets;

[NavigationMetaData(
	Title = "Coinjoin Settings",
	Category = "Coinjoin",
	Caption = "Coinjoin Settings",
	IconName = "wallet_action_coinjoin",
	Keywords = new [] { "Coinjoin", "Settings", "Options", "Pleb", "Auto" },
	Searchable = true)
]
public partial class CoinJoinSettingsViewModel : RoutableViewModel
{
	private readonly Wallet _wallet;
	[AutoNotify] private bool _autoCoinJoin;
	[AutoNotify] private bool _isCoinjoinProfileSelected;
	[AutoNotify] private string _plebStopThreshold;
	[AutoNotify] private string? _selectedCoinjoinProfileName;

	public CoinJoinSettingsViewModel(WalletViewModelBase walletViewModelBase)
	{
		_wallet = walletViewModelBase.Wallet;
		_autoCoinJoin = _wallet.KeyManager.AutoCoinJoin;
		_plebStopThreshold = _wallet.KeyManager.PlebStopThreshold?.ToString() ??
		                     KeyManager.DefaultPlebStopThreshold.ToString();

		SetupCancel(enableCancel: false, enableCancelOnEscape: true, enableCancelOnPressed: true);

		NextCommand = CancelCommand;

		SetAutoCoinJoin = ReactiveCommand.CreateFromTask(
			async () =>
			{
				if (_wallet.KeyManager.IsCoinjoinProfileSelected)
				{
					AutoCoinJoin = !AutoCoinJoin;
				}
				else
				{
					await NavigateDialogAsync(
						new CoinJoinProfilesViewModel(_wallet.KeyManager, false),
						NavigationTarget.DialogScreen);
				}

				if (_wallet.KeyManager.IsCoinjoinProfileSelected)
				{
					_wallet.KeyManager.AutoCoinJoin = AutoCoinJoin;
					_wallet.KeyManager.ToFile();
				}
				else
				{
					AutoCoinJoin = false;
				}
			});

		SelectCoinjoinProfileCommand = ReactiveCommand.CreateFromTask(SelectCoinjoinProfileAsync);

		this.ValidateProperty(x => x.PlebStopThreshold, ValidatePlebStopThreshold);

		this.WhenAnyValue(x => x.PlebStopThreshold)
			.Skip(1)
			.Throttle(TimeSpan.FromMilliseconds(1000))
			.ObserveOn(RxApp.TaskpoolScheduler)
			.Subscribe(
				x =>
				{
					if (Money.TryParse(x, out var result) && result != _wallet.KeyManager.PlebStopThreshold)
					{
						_wallet.KeyManager.PlebStopThreshold = result;
						_wallet.KeyManager.ToFile();
					}
				});
	}

	public ICommand SetAutoCoinJoin { get; }

	public ICommand SelectCoinjoinProfileCommand { get; }

	protected override void OnNavigatedTo(bool isInHistory, CompositeDisposable disposables)
	{
		base.OnNavigatedTo(isInHistory, disposables);
		PlebStopThreshold = _wallet.KeyManager.PlebStopThreshold.ToString();

		IsCoinjoinProfileSelected = _wallet.KeyManager.IsCoinjoinProfileSelected;
		SelectedCoinjoinProfileName =
			(_wallet.KeyManager.IsCoinjoinProfileSelected,
					CoinJoinProfilesViewModel.IdentifySelectedProfile(_wallet.KeyManager)) switch
				{
					(true, CoinJoinProfileViewModelBase x) => x.Title,
					(false, _) => "None",
					_ => "Unknown"
				};
	}

	private static void ValidatePlebStopThreshold(IValidationErrors errors, string plebStopThreshold)
	{
		if (string.IsNullOrWhiteSpace(plebStopThreshold) || string.IsNullOrEmpty(plebStopThreshold))
		{
			return;
		}

		if (plebStopThreshold.Contains(',', StringComparison.InvariantCultureIgnoreCase))
		{
			errors.Add(ErrorSeverity.Error, "Use decimal point instead of comma.");
		}
		else if (!decimal.TryParse(plebStopThreshold, out _))
		{
			errors.Add(ErrorSeverity.Error, "Invalid coinjoin threshold.");
		}
	}

	private async Task SelectCoinjoinProfileAsync()
	{
		await NavigateDialogAsync(
			new CoinJoinProfilesViewModel(_wallet.KeyManager, false),
			NavigationTarget.DialogScreen);
		AutoCoinJoin = _wallet.KeyManager.AutoCoinJoin;
	}

	private void ValidatePlebStopThreshold(IValidationErrors errors)
	{
		ValidatePlebStopThreshold(errors, PlebStopThreshold);
	}
}
