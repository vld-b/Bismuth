using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace WID
{
    public class PendingFileOperationsSystem
    {
        public bool isLocked { get; private set; } = false;

        private List<string> pendingCreations = new List<string>();
        private List<string> pendingDeletions = new List<string>();
        private List<StorageFile> pendingMoves = new List<StorageFile>();
        private List<RenameItem> pendingRenames = new List<RenameItem>();

        private List<string> pendingCreationsLocked = new List<string>();
        private List<string> pendingDeletionsLocked = new List<string>();
        private List<StorageFile> pendingMovesLocked = new List<StorageFile>();
        private List<RenameItem> pendingRenamesLocked = new List<RenameItem>();

        private List<string> pendingCreationsDeletions = new List<string>();
        private List<string> pendingDeletionsDeletions = new List<string>();
        private List<StorageFile> pendingMovesDeletions = new List<StorageFile>();
        private List<RenameItem> pendingRenamesDeletions = new List<RenameItem>();
        public StorageFolder notebookFolder { get; set; }

        public PendingFileOperationsSystem(StorageFolder notebookFolder)
        {
            this.notebookFolder = notebookFolder;
        }

        public async Task CreatePending()
        {
            if (!isLocked)
                Unlock(); // Make sure Locked list items are in unlocked lists

            await Utils.CreatePending(pendingCreations, notebookFolder);
        }

        public async Task ExecuteRestPending()
        {
            if (!isLocked)
                Unlock(); // Make sure locked list items are in unlocked lists

            await Utils.DeletePending(pendingDeletions, notebookFolder);
            await Utils.MovePending(pendingMoves, notebookFolder);
            await Utils.RenamePending(pendingRenames);
        }

        public void AddPendingCreations(string item)
        {
            if (isLocked)
                pendingCreationsLocked.Add(item);
            else
                pendingCreations.Add(item);
        }

        public void AddPendingDeletions(string item)
        {
            if (isLocked)
                pendingDeletionsLocked.Add(item);
            else
                pendingDeletions.Add(item);
        }

        public void AddPendingMoves(StorageFile item)
        {
            if (isLocked)
                pendingMovesLocked.Add(item);
            else
                pendingMoves.Add(item);
        }

        public void AddPendingRenames(RenameItem item)
        {
            if (isLocked)
                pendingRenamesLocked.Add(item);
            else
                pendingRenames.Add(item);
        }

        public void RemovePendingCreations(string item)
        {
            if (isLocked)
                pendingCreationsDeletions.Add(item);
            else
                pendingCreations.Remove(item);
        }

        public void RemovePendingDeletions(string item)
        {
            if (isLocked)
                pendingDeletionsDeletions.Add(item);
            else
                pendingDeletions.Remove(item);
        }

        public void RemovePendingMoves(StorageFile item)
        {
            if (isLocked)
                pendingMovesDeletions.Add(item);
            else
                pendingMoves.RemoveAll((s) => { return s.Path == item.Path; });
        }

        public void RemovePendingRenames(RenameItem item)
        {
            if (isLocked)
                pendingRenamesDeletions.Add(item);
            else
                pendingRenames.RemoveAll((s) => { return s.to == item.to; });
        }

        public void Lock()
        {
            isLocked = true;
        }

        public void Unlock()
        {
            isLocked = false;

            pendingCreations.Add(pendingCreationsLocked);
            pendingCreationsLocked.Clear();
            while (pendingCreationsDeletions.Count > 0)
                pendingCreations.Remove(pendingCreationsDeletions.Pop(0));

            pendingDeletions.Add(pendingDeletionsLocked);
            pendingDeletionsLocked.Clear();
            while (pendingDeletionsDeletions.Count > 0)
                pendingDeletions.Remove(pendingDeletionsDeletions.Pop(0));

            pendingMoves.Add(pendingMovesLocked);
            pendingMovesLocked.Clear();
            while (pendingMovesDeletions.Count > 0)
                pendingMoves.RemoveAll((s) => { return s.Path == pendingMovesDeletions.Pop(0).Path; });

            pendingRenames.Add(pendingRenamesLocked);
            pendingRenamesLocked.Clear();
            while (pendingRenamesDeletions.Count > 0)
                pendingRenames.Remove(pendingRenamesDeletions.Pop(0));
        }
    }
}
