using System.Threading.Tasks;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.WabiSabi.Backend.Banning;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.WabiSabi.Backend;

public class UtxoWhitelistTests
{
	[Fact]
	public async Task WhitelistChangeTrafficAsync()
	{
		var whitelist = new Whitelist();
		var currentChangeId = whitelist.ChangeId;

		var outpoint = BitcoinFactory.CreateOutPoint();
		whitelist.Add(outpoint);
		Assert.NotEqual(currentChangeId, whitelist.ChangeId);
		currentChangeId = whitelist.ChangeId;

		var outpoint2 = BitcoinFactory.CreateOutPoint();
		whitelist.Add(outpoint2);
		Assert.NotEqual(currentChangeId, whitelist.ChangeId);
		currentChangeId = whitelist.ChangeId;

		Assert.True(whitelist.TryRelease(outpoint));
		Assert.NotEqual(currentChangeId, whitelist.ChangeId);

		var outpoint3 = BitcoinFactory.CreateOutPoint();
		whitelist.Add(outpoint3);
		Assert.NotEqual(currentChangeId, whitelist.ChangeId);

		await Task.Delay(1000);
		whitelist.RemoveAllExpired(TimeSpan.FromSeconds(1));

		Assert.Equal(0, whitelist.CountInnocents());
	}
}
