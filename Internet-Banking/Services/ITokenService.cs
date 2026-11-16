using Internet_Banking.Models;

namespace Internet_Banking.Services;

public interface ITokenService
{
    string CreateToken(Customer customer);
}

