﻿using System;
using System.Linq;
using Rebus.Extensions;
using Rebus.Topic;

namespace Rebus.AzureServiceBus
{
    /// <summary>
    /// Helper responsible for implementing how various names turn out
    /// </summary>
    public class AzureServiceBusNameHelper : ITopicNameConvention
    {
        readonly char[] _additionalValidCharacters;
        readonly bool _lowerCase;
        readonly bool _useLegacyNaming;

        /// <summary>
        /// Creates the name helper, using legacy naming (more conservative escaping, lowercase throughout) if <paramref name="useLegacyNaming"/> is true
        /// </summary>
        public AzureServiceBusNameHelper(bool useLegacyNaming = false)
        {
            _additionalValidCharacters = useLegacyNaming
                ? new[] { '_' }
                : new[] { '.', '-', '_' };

            _lowerCase = useLegacyNaming;
            _useLegacyNaming = useLegacyNaming;
        }

        /// <summary>
        /// Gets a topic name from the given <paramref name="eventType"/>
        /// </summary>
        public string GetTopic(Type eventType)
        {
            if (!_useLegacyNaming)
            {
                var assemblyName = ReplaceInvalidCharacters(eventType.Assembly.GetName().Name);
                var typeName = ReplaceInvalidCharacters(eventType.FullName);

                return $"{assemblyName}/{typeName}";
            }

            var simpleAssemblyQualifiedName = eventType.GetSimpleAssemblyQualifiedName();

            var topicName = ReplaceInvalidCharacters(simpleAssemblyQualifiedName);

            return topicName;
        }

        /// <summary>
        /// "Sanitizes" the string <paramref name="str"/> to make it into a valid ASB entity name
        /// </summary>
        public string ReplaceInvalidCharacters(string str)
        {
            var name = string.Concat(str.Select(c =>
            {
                if (c == '/') return '/';

                return IsValidCharacter(c) ? c : '_';
            }));

            return _lowerCase
                ? name.ToLowerInvariant()
                : name;
        }

        /// <summary>
        /// Checks that the <paramref name="queueName"/> is in fact valie, throwing an exception if it is not
        /// </summary>
        public void EnsureIsValidQueueName(string queueName)
        {
            var segments = queueName.Split('/');

            if (segments.All(segment => segment.All(IsValidCharacter))) return;

            throw new ArgumentException($"The string '{queueName}' is not a valid Azure Service Bus queue name! ASB queue names must consist of only letters (a-z and A-Z), digits (0-9), hyphens (-), periods (.), and underscores (_), and possibly multiple segments of these separated by slash (/).");
        }

        bool IsValidCharacter(char c)
        {
            return char.IsLetterOrDigit(c)
                   || _additionalValidCharacters.Contains(c);
        }
    }
}