using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mishin870.MHScript.engine.documentation {
    class FunctionEntry : IDocumentationEntry {
        public string name;
        public string description;
        public bool isLocal;

        public FunctionEntry(string name, string description, bool isLocal) {
            this.name = name;
            this.description = description;
            this.isLocal = isLocal;
        }

        public string getTitle() {
            return name;
        }

        public string getDescription() {
            return description;
        }

        public DocumentationTypes getType() {
            return DocumentationTypes.FUNCTION;
        }

        override public string ToString() {
            return "[FunctionEntry name=" + name + ", description=" + description + ", isLocal=" + isLocal + "]";
        }
    }
}
