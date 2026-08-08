using FluentValidation;

namespace DUSTER.EComm.Data.CommonClass
{
    public class ValidationModel
    {
        public ValidationModel() { }

        public IValidator ValidateModel { get; set; }
        public BaseEntity Model { get; set; }
    }
}
