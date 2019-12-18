const
  gulp = requireModule("gulp-with-help"),
  isPackMaster = require("./modules/is-pack-master-branch"),
  runSequence = requireModule("run-sequence");

gulp.task("release", ["build-for-release"], async (done) => {
  const onPackMasterBranch = await isPackMaster();
  return onPackMasterBranch
    ? runSequencePromise("test", "pack", "commit-release", "tag", "push-tags")
    : runSequencePromise("test", "pack");

  function runSequencePromise() {
      var targets = Array.from(arguments);
      targets.push(done);
      runSequence.apply(null, targets);
  }
});

