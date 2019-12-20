const
  gulp = requireModule("gulp-with-help"),
  chalk = require("chalk"),
  isPackMaster = require("./modules/is-pack-master-branch"),
  canPush = require("./modules/can-push"),
  runSequence = requireModule("run-sequence");

gulp.task("release", ["build-for-release"], async () => {
  console.log("--- start actual release task ---");
  const
    onPackMasterBranch = await isPackMaster(),
    pushAllowed = await canPush(),
    fullRelease = onPackMasterBranch && pushAllowed;
  if (!fullRelease) {
    const
      remote = process.env.GIT_REMOTE || process.env.GIT_OVERRIDE_REMOTE || "origin",
      branch = process.env.GIT_BRANCH || process.env.GIT_OVERRIDE_BRANCH || "master"
    reason = onPackMasterBranch
      ? `Unable to push to remote ${remote} branch ${branch}`
      : `Not on pack master branch `
    console.error(`Unable to perform "full" release with tags: `);
  }
  return fullRelease
    ? runSequencePromise("cover-dotnet", "pack", "commit-release", "tag", "push-tags")
    : runSequencePromise("cover-dotnet", "pack");

  function runSequencePromise() {
    console.log("creating a sequence...");
    return new Promise((resolve, reject) => {
      var targets = Array.from(arguments);
      targets.push(resolve);
      console.log(targets);
      runSequence.apply(null, targets);
    });
  }
});

