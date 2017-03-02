using UnityEngine;
using System;

namespace RAGE.Analytics.Exceptions{

    public class XApiException : TrackerException{
        public enum XApiExceptionType { VERB, ACTOR, TARGET, EXTENSION };

        public XApiExceptionType Type {
            get;
            private set;
        }

        public XApiException(string message, XApiExceptionType type) : base(message){
            this.Type = type;
        }
    }
}