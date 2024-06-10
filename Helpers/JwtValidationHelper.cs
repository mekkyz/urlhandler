using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace urlhandler.Helpers;

public abstract class JwtValidationHelper {
  public static bool IsTokenValid(string token) {
    var currentLocalTime = DateTime.Now;
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);
    var expClaim = jwtToken?.Claims.FirstOrDefault(claim => claim.Type == "exp");
    if (expClaim == null) return false;
    if (!long.TryParse(expClaim.Value, out var expUnixTime)) return false;
    var expTime = DateTimeOffset.FromUnixTimeSeconds(expUnixTime).DateTime;
    return currentLocalTime < expTime;
  }
}
