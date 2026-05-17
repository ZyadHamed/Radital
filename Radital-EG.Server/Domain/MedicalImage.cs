using Domain.People;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class MedicalImage : IdentifiableEntity
    {
        public required Patient Patient { get; set; }
        public ImageModalitiesEnum ImageModality { get; set; }
        public string StorageReference { get; set; }
    }
}
