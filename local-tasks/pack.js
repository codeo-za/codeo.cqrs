const gulp = requireModule("gulp-with-help"),
  packageDir = require("./config").packageDir,
  runSequence = requireModule("run-sequence"),
  spawn = requireModule("spawn");

gulp.task("prepare-pack", done => {
  runSequence("build-for-release", "increment-package-version", done);
});

gulp.task("pack", ["prepare-pack"], () => {
  return doPack();
});

function doPack() {
  return spawn("tools/nuget.exe", [
    "pack",
    "src/RetailStudio.ApiClient/Package.nuspec",
    "-OutputDirectory",
    packageDir
  ]);
}
