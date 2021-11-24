using AutoNotify;

namespace app
{
    public partial class ExampleViewModel
    {
        [AutoNotify]
        private string text;
        
        [AutoNotify]
        private int code;
	}
}