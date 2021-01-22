const
  gulp = requireModule("gulp-with-help"),
  gutil = requireModule("gulp-util"),
  editXml = require("gulp-edit-xml"),
  config = require("./config"),
  env = requireModule("env"),
  promisify = requireModule("promisify-stream"),
  chalk = require("chalk"),
  canPush = require("./modules/can-push");
  isPackMasterBranch = require("./modules/is-pack-master-branch"),
  { incrementPackageVersion } = requireModule("gulp-increment-nuget-package-version"),
  containingFolder = `src/${config.packageProject}`;

gulp.task("increment-package-version", async () => {
  const onPackMaster = await isPackMasterBranch();
  if (!onPackMaster) {
    console.warn(chalk.yellow(`WARNING: not incrementing package version: not on pack master branch "${env.resolve("PACK_MASTER")}"`));
    return;
  }
  const pushAllowed = await canPush();
  if (!pushAllowed) {
    console.warn(chalk.yellow(`WARNING: unable to push changes, bailing out on package version increment`));
    return;
  }

  return promisify(
    gulp.src(`${containingFolder}/Codeo.CQRS.csproj`)
      .pipe(incrementPackageVersion)
      .pipe(gulp.dest(containingFolder))
  );
});

function testNaN(version) {
  Object.keys(version).forEach(k => {
    if (isNaN(version[k])) {
      throw new Error(`${k} is not an integer`);
    }
  });
}