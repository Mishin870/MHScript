using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mishin870.MHScript.engine.documentation {
    public interface IDocumentationEntry {
        string getTitle();
        string getDescription();
        DocumentationTypes getType();
    }
}
