using AutoNotify;

namespace app
{
    public partial class ExampleViewModel
    {
        [AutoNotify]
        private string _text = "private field text";

        [AutoNotify(PropertyName = "Count")]
        private int _amount = 5;
    }
}
