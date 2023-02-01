using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibOrbisPkg.PKG;
using LibOrbisPkg.PFS;
using System.IO;
using System.IO.MemoryMappedFiles;
using LibOrbisPkg.Util;
using LibOrbisPkg.GP4;

namespace LibOrbisPkg.Kimie
{
    /// <summary>
    /// Contains functionality to create a GP4 from a PKG
    /// </summary>
    public class Counter
    {
        public static EntryId[] GeneratedEntries = new[]
        {
      EntryId.DIGESTS,
      EntryId.ENTRY_KEYS,
      EntryId.IMAGE_KEY,
      EntryId.GENERAL_DIGESTS,
      EntryId.METAS,
      EntryId.ENTRY_NAMES,
      EntryId.LICENSE_DAT,
      EntryId.LICENSE_INFO,
      EntryId.PSRESERVED_DAT,
      EntryId.PLAYGO_CHUNK_DAT,
      EntryId.PLAYGO_CHUNK_SHA,
      EntryId.PLAYGO_MANIFEST_XML,
    };

        public int CountPkfFiles(MemoryMappedFile pkgFile, string passcode = null)
        {
            string FilePath = Path.Combine("C:\\Users\\Kimiegg\\Downloads\\PKG Editor Test\\", ".progress.txt");
            
            
            int count = 0;
            int dirCount = 0;
            Pkg pkg;
            using (var f = pkgFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                pkg = new PkgReader(f).ReadPkg();

            passcode = passcode ?? "00000000000000000000000000000000";

            var project = Gp4Project.Create(ContentTypeToVolumeType(pkg.Header.content_type));
            project.volume.Package.Passcode = passcode;
            project.volume.Package.ContentId = pkg.Header.content_id;
            project.volume.Package.AppType = project.volume.Type == VolumeType.pkg_ps4_app ? "full" : null;
            project.volume.Package.StorageType = project.volume.Type == VolumeType.pkg_ps4_app ? "digital50" : null;


            foreach (var meta in pkg.Metas.Metas)
            {
                if (GeneratedEntries.Contains(meta.id)) continue;
                if (!EntryNames.IdToName.ContainsKey(meta.id)) continue;
                count++;
            }
            byte[] ekpfs;
            if (pkg.CheckPasscode(passcode))
            {
                ekpfs = Crypto.ComputeKeys(pkg.Header.content_id, passcode, 1);
            }
            else
            {
                ekpfs = pkg.GetEkpfs();
            }
            using (var va = pkgFile.CreateViewAccessor((long)pkg.Header.pfs_image_offset, (long)pkg.Header.pfs_image_size, MemoryMappedFileAccess.Read))
            {
                var outerPfs = new PfsReader(va, pkg.Header.pfs_flags, ekpfs);
                var inner = new PfsReader(new PFSCReader(outerPfs.GetFile("pfs_image.dat").GetView()));
                // Convert PFS image timestamp from UNIX time and save it in the project
                project.volume.TimeStamp = new DateTime(1970, 1, 1)
                  .AddSeconds(inner.Header.InodeBlockSig.Time1_sec);
                var uroot = inner.GetURoot();
                Dir dir = null;
                var projectDirs = new Queue<Dir>();
                var pfsDirs = new Queue<PfsReader.Dir>();
                pfsDirs.Enqueue(uroot);
                projectDirs.Enqueue(dir);
                while (pfsDirs.Count > 0)
                {
                    dir = projectDirs.Dequeue();
                    foreach (var f in pfsDirs.Dequeue().children)
                    {
                        if (f is PfsReader.Dir d)
                        {
                            pfsDirs.Enqueue(d);
                            projectDirs.Enqueue(project.AddDir(dir, d.name));
                            dirCount++;
                        }
                        else if (f is PfsReader.File file)
                        {
                            count++;
                        }
                        
                    }
                }
            }
            return count;
        }
        private static VolumeType ContentTypeToVolumeType(ContentType t)
        {
            switch (t)
            {
                case ContentType.GD:
                    return VolumeType.pkg_ps4_app;
                case ContentType.DP:
                    return VolumeType.pkg_ps4_patch;
                case ContentType.AC:
                    return VolumeType.pkg_ps4_ac_data;
                case ContentType.AL:
                    return VolumeType.pkg_ps4_ac_nodata;
                default:
                    return 0;
            }
        }
        
    }
}
