using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizerWebJob
{
    internal class KeyedLock
    {
        private HashSet<string> _acquiredLocks = new HashSet<string>();

        public bool TryAcquireLock(string key)
        {
            lock (_acquiredLocks)
            {
                if (!_acquiredLocks.Contains(key))
                {
                    _acquiredLocks.Add(key);
                    return true;
                }
            }
            return false;
        }

        public void ReleaseLock(string key)
        {
            lock (_acquiredLocks)
            {
                _acquiredLocks.Remove(key);
            }
        }
        

    }
}
