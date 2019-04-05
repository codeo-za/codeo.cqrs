const
  gulp = requireModule("gulp-with-help"),
  packageDir = require("./config").packageDir,
  path = require("path"),
  fs = require("fs"),
  runSequence = requireModule("run-sequence"),
  spawn = requireModule("spawn");

gulp.task("release", [ "build-for-release" ], (done) => {
  return runSequence(
    "pack",
    "commit-release",
    "tag",
    done
  );
});
