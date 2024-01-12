const
  readCurrentBranch = requireModule("./read-current-git-branch"),
  env = requireModule("env");
env.register({
  name: "PACK_MASTER",
  help: "branch which serves as the stable master for package sources; other branches have a version suffix added to the package version",
  default: "master"
});

module.exports = async function isPackMasterBranch() {
  const
    current = await readCurrentBranch(),
    packMaster = env.resolve("PACK_MASTER");
  return packMaster === current;
}

