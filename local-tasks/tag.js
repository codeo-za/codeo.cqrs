const gulp = requireModule("gulp-with-help"),
  gutil = requireModule("gulp-util"),
  editXml = require("gulp-edit-xml"),
  Git = require("simple-git"),
  git = new Git(),
  config = require("./config"),
  canPush = require("./modules/can-push"),
  containingFolder = `src/${config.packageProject}`;

gulp.task("tag", () => {
  return new Promise(async (resolve, reject) => {
    const pushAllowed = await canPush();
    if (!pushAllowed) {
      reject("Push not allowed to remote; refusing to tag");
    }
    gulp.src(`${containingFolder}/Package.nuspec`).pipe(
      editXml(xml => {
        const node = xml.package.metadata[0].version,
          version = node[0].trim();

        gutil.log(gutil.colors.cyan(`Tagging at: "v${version}"`));
        gitTag(`v${version}`, `:bookmark: ${version}`)
          .then(() => {
            resolve();
          })
          .catch(err => reject(err));
        return xml;
      })
    );
  });
});

gulp.task("push-tags", "Pushes tags and commits", async () => {
  return gitPushTags()
    .then(() => gitPush())
    .then(() =>
      gutil.log(gutil.colors.green("-> all commits and tags pushed!"))
    );
});

function gitTag(tag, comment) {
  return new Promise(async (resolve, reject) => {
    git.addAnnotatedTag(tag, comment, err => {
      if (err) {
        return reject(err);
      }
      resolve();
    });
  });
}

function gitPushTags() {
  return new Promise((resolve, reject) => {
    gutil.log(gutil.colors.green("pushing tags..."));
    git.pushTags("origin", err => {
      if (err) {
        return reject(err);
      }
      resolve();
    });
  });
}

function gitPush() {
  return new Promise((resolve, reject) => {
    gutil.log(gutil.colors.green("pushing local commits..."));
    git.push("origin", "master", err => {
      if (err) {
        return reject(err);
      }
      resolve();
    });
  });
}
