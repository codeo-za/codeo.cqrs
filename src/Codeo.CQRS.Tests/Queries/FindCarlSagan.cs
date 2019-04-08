namespace Codeo.CQRS.Tests.Queries
{
    public class FindCarlSagan : FindPersonByName
    {
        public FindCarlSagan() : base("Carl Sagan")
        {
        }
    }
}